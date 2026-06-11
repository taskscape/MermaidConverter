using System.Text.RegularExpressions;

namespace MermaidToDiagrams.Shared;

public sealed class EligibilityChecker
{
    private static readonly Regex GraphRegex = new(@"^\s*(flowchart|graph)\s+(LR|RL|TB|TD|BT)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex AzureNodeRegex = new(@"\baz_([a-z0-9]+)_([A-Za-z0-9_]+)__([A-Za-z0-9_]+)\b", RegexOptions.Compiled);
    private static readonly Regex MermaidNodeLikeRegex = new(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(?:\[|\(|--|-.|==)", RegexOptions.Multiline | RegexOptions.Compiled);

    public IReadOnlyList<EligibilityIssue> Check(string? source)
    {
        var issues = new List<EligibilityIssue>();
        if (string.IsNullOrWhiteSpace(source))
        {
            issues.Add(EligibilityIssue.Error("Mermaid source is empty."));
            return issues;
        }

        if (!GraphRegex.IsMatch(source))
        {
            issues.Add(EligibilityIssue.Error("The source must contain a Mermaid 'flowchart' or 'graph' declaration with direction LR, RL, TB, TD, or BT."));
        }

        if (!source.Contains("%% m2d: strict = true %%", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(EligibilityIssue.Error("Missing required decorator: %% m2d: strict = true %%"));
        }

        if (!source.Contains("%% m2d: convention = az_<category>_<ClassName>__<logicalId> %%", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(EligibilityIssue.Error("Missing required convention decorator: %% m2d: convention = az_<category>_<ClassName>__<logicalId> %%"));
        }

        if (!source.Contains("%% m2d: title =", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(EligibilityIssue.Warning("Missing recommended title decorator: %% m2d: title = My Architecture %%"));
        }

        var azureNodes = AzureNodeRegex.Matches(source);
        if (azureNodes.Count == 0)
        {
            issues.Add(EligibilityIssue.Error("No deterministic Azure node IDs were found. Expected IDs like az_compute_KubernetesServices__aks."));
        }

        foreach (Match match in MermaidNodeLikeRegex.Matches(source))
        {
            var id = match.Groups[1].Value;
            if (id is "flowchart" or "graph" or "subgraph" or "end")
            {
                continue;
            }

            if (!id.StartsWith("az_", StringComparison.Ordinal) || !AzureNodeRegex.IsMatch(id))
            {
                issues.Add(EligibilityIssue.Error($"Node '{id}' does not follow az_<category>_<ClassName>__<logicalId>."));
            }
        }

        if (source.Contains("<--", StringComparison.Ordinal) || source.Contains("<->", StringComparison.Ordinal))
        {
            issues.Add(EligibilityIssue.Error("Reverse and bidirectional Mermaid arrows are not supported. Use directed '-->' edges."));
        }

        return issues;
    }
}

public enum EligibilitySeverity
{
    Warning,
    Error
}

public sealed record EligibilityIssue(EligibilitySeverity Severity, string Message)
{
    public static EligibilityIssue Error(string message) => new(EligibilitySeverity.Error, message);
    public static EligibilityIssue Warning(string message) => new(EligibilitySeverity.Warning, message);
    public string ToDisplayText() => $"{Severity}: {Message}";
}
