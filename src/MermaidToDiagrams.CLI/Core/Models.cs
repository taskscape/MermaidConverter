namespace MermaidToDiagrams.Core;

internal enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

internal sealed record Diagnostic(DiagnosticSeverity Severity, string Message, int Line = 0, int Column = 0)
{
    public static Diagnostic Error(string message, int line = 0, int column = 0) => new(DiagnosticSeverity.Error, Format(message, line, column), line, column);
    public static Diagnostic Warning(string message, int line = 0, int column = 0) => new(DiagnosticSeverity.Warning, Format(message, line, column), line, column);

    private static string Format(string message, int line, int column)
    {
        return line > 0 ? $"{message} at line {line}, column {column}" : message;
    }
}

internal sealed class MermaidDiagram
{
    public string Title { get; set; } = "Azure architecture";
    public string Direction { get; set; } = "LR";
    public List<NodeModel> Nodes { get; } = [];
    public List<EdgeModel> Edges { get; } = [];
    public List<Diagnostic> Diagnostics { get; } = [];
}

internal sealed class NodeModel
{
    public required string Id { get; init; }
    public required string Label { get; set; }
    public string? Cluster { get; set; }
    public int Line { get; init; }
    public AzureIcon? Icon { get; set; }
}

internal sealed class EdgeModel
{
    public required string From { get; init; }
    public required string To { get; init; }
    public string? Label { get; init; }
    public string Style { get; init; } = "solid";
    public int Line { get; init; }
}

internal sealed record AzureIcon(
    string Category,
    string ClassName,
    string ImportPath,
    IReadOnlyList<string> Aliases);

internal sealed record EmitOptions(string OutputBasePath, string Format, string Theme);

internal sealed record ConversionResult(bool Success, string PythonScript, IReadOnlyList<Diagnostic> Diagnostics)
{
    public static ConversionResult Successful(string script, IReadOnlyList<Diagnostic> diagnostics) => new(true, script, diagnostics);
    public static ConversionResult Failed(IReadOnlyList<Diagnostic> diagnostics) => new(false, "", diagnostics);
}

internal sealed record ResolvedDiagram(MermaidDiagram Diagram, IReadOnlyList<Diagnostic> Diagnostics);
