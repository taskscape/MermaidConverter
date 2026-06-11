using System.Text.RegularExpressions;

namespace MermaidToDiagrams.Core;

internal sealed partial class MermaidParser
{
    private static readonly HashSet<string> SupportedDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        "TB", "TD", "BT", "LR", "RL"
    };

    public MermaidDiagram Parse(string source)
    {
        var diagram = new MermaidDiagram();
        var clusters = new Stack<string>();
        var nodesById = new Dictionary<string, NodeModel>(StringComparer.Ordinal);
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index].Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("%%", StringComparison.Ordinal))
            {
                ParseMetadata(line, diagram);
                continue;
            }

            var graphMatch = GraphDeclarationRegex().Match(line);
            if (graphMatch.Success)
            {
                var direction = graphMatch.Groups["direction"].Value;
                if (!SupportedDirections.Contains(direction))
                {
                    diagram.Diagnostics.Add(Diagnostic.Error($"Unsupported flowchart direction '{direction}'", lineNumber, graphMatch.Groups["direction"].Index + 1));
                }
                else
                {
                    diagram.Direction = direction.Equals("TD", StringComparison.OrdinalIgnoreCase) ? "TB" : direction.ToUpperInvariant();
                }

                continue;
            }

            var subgraphMatch = SubgraphRegex().Match(line);
            if (subgraphMatch.Success)
            {
                var clusterLabel = subgraphMatch.Groups["label"].Success
                    ? subgraphMatch.Groups["label"].Value
                    : subgraphMatch.Groups["id"].Value;
                clusters.Push(clusterLabel);
                continue;
            }

            if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                if (clusters.Count == 0)
                {
                    diagram.Diagnostics.Add(Diagnostic.Error("Unexpected 'end' without matching subgraph", lineNumber, 1));
                }
                else
                {
                    clusters.Pop();
                }

                continue;
            }

            if (TryParseEdge(line, lineNumber, diagram, nodesById, clusters))
            {
                continue;
            }

            if (TryParseNode(line, lineNumber, diagram, nodesById, clusters))
            {
                continue;
            }

            if (line.StartsWith("class ", StringComparison.Ordinal) || line.StartsWith("classDef ", StringComparison.Ordinal))
            {
                diagram.Diagnostics.Add(Diagnostic.Warning("Mermaid class directives are accepted but ignored by this renderer", lineNumber, 1));
                continue;
            }

            diagram.Diagnostics.Add(Diagnostic.Error($"Unsupported Mermaid statement: {line}", lineNumber, 1));
        }

        if (clusters.Count > 0)
        {
            diagram.Diagnostics.Add(Diagnostic.Error("One or more subgraph blocks were not closed with 'end'"));
        }

        return diagram;
    }

    private static void ParseMetadata(string line, MermaidDiagram diagram)
    {
        var match = MetadataRegex().Match(line);
        if (match.Success && match.Groups["key"].Value.Equals("title", StringComparison.OrdinalIgnoreCase))
        {
            diagram.Title = match.Groups["value"].Value.Trim();
        }
    }

    private static bool TryParseNode(string line, int lineNumber, MermaidDiagram diagram, Dictionary<string, NodeModel> nodesById, Stack<string> clusters)
    {
        var match = NodeRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        var id = match.Groups["id"].Value;
        var label = UnquoteLabel(match.Groups["label"].Success ? match.Groups["label"].Value : id);
        EnsureNode(id, label, lineNumber, diagram, nodesById, clusters);
        return true;
    }

    private static bool TryParseEdge(string line, int lineNumber, MermaidDiagram diagram, Dictionary<string, NodeModel> nodesById, Stack<string> clusters)
    {
        var match = EdgeRegex().Match(line);
        if (!match.Success)
        {
            return false;
        }

        var from = match.Groups["from"].Value;
        var to = match.Groups["to"].Value;
        var edgeToken = match.Groups["edge"].Value;
        var label = match.Groups["label"].Success ? match.Groups["label"].Value : null;

        EnsureNode(from, from, lineNumber, diagram, nodesById, clusters);
        EnsureNode(to, to, lineNumber, diagram, nodesById, clusters);

        if (edgeToken.Contains('<', StringComparison.Ordinal))
        {
            diagram.Diagnostics.Add(Diagnostic.Error("Reverse or bidirectional edges are not supported in v1; use '-->' from source to target", lineNumber, match.Groups["edge"].Index + 1));
            return true;
        }

        diagram.Edges.Add(new EdgeModel
        {
            From = from,
            To = to,
            Label = label,
            Style = edgeToken.Contains('.', StringComparison.Ordinal) ? "dotted" : edgeToken.Contains('=', StringComparison.Ordinal) ? "bold" : "solid",
            Line = lineNumber
        });

        return true;
    }

    private static NodeModel EnsureNode(string id, string label, int lineNumber, MermaidDiagram diagram, Dictionary<string, NodeModel> nodesById, Stack<string> clusters)
    {
        if (nodesById.TryGetValue(id, out var existing))
        {
            if (existing.Label == existing.Id && label != id)
            {
                existing.Label = label;
            }

            return existing;
        }

        var node = new NodeModel
        {
            Id = id,
            Label = label,
            Cluster = clusters.Count > 0 ? string.Join("/", clusters.Reverse()) : null,
            Line = lineNumber
        };
        nodesById[id] = node;
        diagram.Nodes.Add(node);
        return node;
    }

    private static string UnquoteLabel(string label)
    {
        return label.Trim().Trim('"').Replace("\\\"", "\"", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^(?:flowchart|graph)\s+(?<direction>[A-Za-z]+)\s*$", RegexOptions.Compiled)]
    private static partial Regex GraphDeclarationRegex();

    [GeneratedRegex(@"^subgraph\s+(?<id>[A-Za-z_][A-Za-z0-9_]*)(?:\s*\[\s*""(?<label>[^""]+)""\s*\])?\s*$", RegexOptions.Compiled)]
    private static partial Regex SubgraphRegex();

    [GeneratedRegex(@"^%%\s*m2d:\s*(?<key>[A-Za-z0-9_.-]+)\s*=\s*(?<value>.*?)\s*%%$", RegexOptions.Compiled)]
    private static partial Regex MetadataRegex();

    [GeneratedRegex(@"^(?<id>[A-Za-z_][A-Za-z0-9_]*)\s*(?:\[\s*""(?<label>[^""]*)""\s*\]|\(\s*""?(?<label>[^"")]+)""?\s*\))?\s*$", RegexOptions.Compiled)]
    private static partial Regex NodeRegex();

    [GeneratedRegex(@"^(?<from>[A-Za-z_][A-Za-z0-9_]*)\s*(?<edge><?[-.=]+>?)\s*(?:\|(?<label>[^|]*)\|)?\s*(?<to>[A-Za-z_][A-Za-z0-9_]*)\s*$", RegexOptions.Compiled)]
    private static partial Regex EdgeRegex();
}
