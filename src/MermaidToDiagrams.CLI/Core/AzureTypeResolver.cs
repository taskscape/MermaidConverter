using System.Text.RegularExpressions;

namespace MermaidToDiagrams.Core;

internal sealed partial class AzureTypeResolver(AzureIconCatalog catalog)
{
    public ResolvedDiagram Resolve(MermaidDiagram diagram, bool strict)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var node in diagram.Nodes)
        {
            var match = AzureNodeIdRegex().Match(node.Id);
            if (match.Success)
            {
                var category = match.Groups["category"].Value;
                var className = match.Groups["class"].Value;
                var icon = catalog.Find(category, className);

                if (icon is null)
                {
                    node.Icon = new AzureIcon(category, className, $"diagrams.azure.{category}.{className}", []);
                    diagnostics.Add(Diagnostic.Warning($"Azure icon type '{category}.{className}' is not in the bundled catalog. The converter will emit diagrams.azure.{category}.{className}; Python rendering will fail if that class is not installed.", node.Line));
                }
                else
                {
                    node.Icon = icon;
                }

                continue;
            }

            var alias = catalog.FindByAlias(node.Id) ?? catalog.FindByAlias(node.Label);
            if (alias is not null)
            {
                node.Icon = alias;
                diagnostics.Add(Diagnostic.Warning($"Node '{node.Id}' used alias-based resolution. Prefer az_{alias.Category}_{alias.ClassName}__{ToLogicalId(node.Id)} for deterministic rendering.", node.Line));
                continue;
            }

            var message = $"Node '{node.Id}' does not use deterministic Azure naming. Expected az_<category>_<ClassName>__<logicalId>.";
            diagnostics.Add(strict ? Diagnostic.Error(message, node.Line) : Diagnostic.Warning(message, node.Line));
        }

        return new ResolvedDiagram(diagram, diagnostics);
    }

    private static string ToLogicalId(string value)
    {
        var chars = value.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_').ToArray();
        return new string(chars);
    }

    [GeneratedRegex(@"^az_(?<category>[a-z0-9]+)_(?<class>[A-Za-z0-9_]+)__(?<logical>[A-Za-z0-9_]+)$", RegexOptions.Compiled)]
    private static partial Regex AzureNodeIdRegex();
}
