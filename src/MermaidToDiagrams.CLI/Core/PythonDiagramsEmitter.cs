using System.Text;
using System.Text.Json;

namespace MermaidToDiagrams.Core;

internal sealed class PythonDiagramsEmitter
{
    public string Emit(MermaidDiagram diagram, EmitOptions options)
    {
        var imports = diagram.Nodes
            .Select(n => n.Icon)
            .Where(i => i is not null)
            .Cast<AzureIcon>()
            .GroupBy(i => ModuleName(i.ImportPath))
            .OrderBy(g => g.Key)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("from diagrams import Diagram, Cluster, Edge");
        foreach (var import in imports)
        {
            builder.Append("from ").Append(import.Key).Append(" import ");
            builder.AppendLine(string.Join(", ", import
                .DistinctBy(i => i.ClassName)
                .OrderBy(i => i.ClassName)
                .Select(i => $"{i.ClassName} as {AliasName(i)}")));
        }

        builder.AppendLine();
        AppendTheme(builder, options.Theme);
        builder.AppendLine();
        builder.AppendLine("nodes = {}");
        builder.AppendLine();
        builder.Append("with Diagram(")
            .Append(PyString(diagram.Title))
            .Append(", filename=")
            .Append(PyString(Path.ChangeExtension(options.OutputBasePath, null) ?? options.OutputBasePath))
            .Append(", outformat=")
            .Append(PyString(options.Format))
            .Append(", show=False, direction=")
            .Append(PyString(diagram.Direction))
            .AppendLine(", graph_attr=graph_attr):");

        var groupedNodes = diagram.Nodes.GroupBy(n => n.Cluster ?? "").OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (var group in groupedNodes)
        {
            if (group.Key.Length == 0)
            {
                foreach (var node in group)
                {
                    AppendNode(builder, node, 1);
                }
            }
            else
            {
                var clusterParts = group.Key.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var indent = 1;
                foreach (var cluster in clusterParts)
                {
                    AppendIndent(builder, indent).Append("with Cluster(").Append(PyString(cluster)).AppendLine("):");
                    indent++;
                }

                foreach (var node in group)
                {
                    AppendNode(builder, node, indent);
                }
            }
        }

        if (diagram.Edges.Count > 0)
        {
            builder.AppendLine();
        }

        foreach (var edge in diagram.Edges)
        {
            AppendEdge(builder, edge);
        }

        return builder.ToString();
    }

    private static void AppendTheme(StringBuilder builder, string theme)
    {
        var graph = theme.Equals("azure-dark", StringComparison.OrdinalIgnoreCase)
            ? new Dictionary<string, string>
            {
                ["bgcolor"] = "#0f172a",
                ["fontname"] = "Segoe UI",
                ["fontcolor"] = "#e2e8f0",
                ["pad"] = "0.45",
                ["nodesep"] = "0.65",
                ["ranksep"] = "0.9",
                ["splines"] = "ortho"
            }
            : new Dictionary<string, string>
            {
                ["bgcolor"] = "transparent",
                ["fontname"] = "Segoe UI",
                ["fontcolor"] = "#0f172a",
                ["pad"] = "0.45",
                ["nodesep"] = "0.65",
                ["ranksep"] = "0.9",
                ["splines"] = "ortho"
            };

        builder.AppendLine("graph_attr = {");
        foreach (var pair in graph)
        {
            builder.Append("    ").Append(PyString(pair.Key)).Append(": ").Append(PyString(pair.Value)).AppendLine(",");
        }
        builder.AppendLine("}");
    }

    private static void AppendNode(StringBuilder builder, NodeModel node, int indent)
    {
        var icon = node.Icon ?? throw new InvalidOperationException($"Node '{node.Id}' was not resolved.");
        AppendIndent(builder, indent)
            .Append("nodes[")
            .Append(PyString(node.Id))
            .Append("] = ")
            .Append(AliasName(icon))
            .Append("(")
            .Append(PyString(node.Label))
            .AppendLine(")");
    }

    private static void AppendEdge(StringBuilder builder, EdgeModel edge)
    {
        AppendIndent(builder, 1)
            .Append("nodes[")
            .Append(PyString(edge.From))
            .Append("] >> Edge(");

        var edgeArgs = new List<string>();
        if (!string.IsNullOrWhiteSpace(edge.Label))
        {
            edgeArgs.Add($"label={PyString(edge.Label)}");
        }

        edgeArgs.Add(edge.Style switch
        {
            "dotted" => "style=\"dotted\"",
            "bold" => "penwidth=\"2.4\"",
            _ => "color=\"#334155\""
        });

        builder.Append(string.Join(", ", edgeArgs));
        builder.Append(") >> nodes[").Append(PyString(edge.To)).AppendLine("]");
    }

    private static StringBuilder AppendIndent(StringBuilder builder, int count)
    {
        return builder.Append(' ', count * 4);
    }

    private static string ModuleName(string importPath)
    {
        var lastDot = importPath.LastIndexOf('.');
        return lastDot > 0 ? importPath[..lastDot] : importPath;
    }

    private static string AliasName(AzureIcon icon)
    {
        return $"Azure_{icon.Category}_{icon.ClassName}";
    }

    private static string PyString(string value)
    {
        return JsonSerializer.Serialize(value);
    }
}
