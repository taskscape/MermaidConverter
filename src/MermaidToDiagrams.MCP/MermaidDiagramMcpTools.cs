using System.ComponentModel;
using System.Text.Json;
using MermaidToDiagrams.Shared;
using ModelContextProtocol.Server;

namespace MermaidToDiagrams.MCP;

[McpServerToolType]
public sealed class MermaidDiagramMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly MermaidConversionService _conversionService;

    public MermaidDiagramMcpTools(MermaidConversionService conversionService)
    {
        _conversionService = conversionService;
    }

    [McpServerTool(Name = "validate_mermaid_azure_diagram")]
    [Description("Validate that a Mermaid payload is eligible for deterministic conversion to an Azure architecture diagram. Returns JSON with validation status, issues, and CLI diagnostics.")]
    public async Task<string> ValidateMermaidAzureDiagram(
        [Description("Mermaid flowchart text containing m2d decorators and deterministic Azure node IDs.")] string mermaid,
        CancellationToken cancellationToken = default)
    {
        var result = await _conversionService.ValidateAsync(mermaid, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            valid = result.Success,
            issues = result.Issues.Select(ToIssueDto),
            cli = ToCliDto(result.CliResult)
        }, JsonOptions);
    }

    [McpServerTool(Name = "convert_mermaid_azure_diagram")]
    [Description("Convert a valid Mermaid Azure architecture payload into a rendered diagram by invoking the MermaidToDiagrams CLI. Returns JSON containing base64 diagram bytes or structured validation/conversion errors.")]
    public async Task<string> ConvertMermaidAzureDiagram(
        [Description("Mermaid flowchart text containing m2d decorators and deterministic Azure node IDs.")] string mermaid,
        [Description("Output format. Supported values: png, svg, pdf. Defaults to png.")] string format = "png",
        [Description("Render theme. Supported values: azure-modern, azure-dark. Defaults to azure-modern.")] string theme = "azure-modern",
        [Description("Include the generated Python Diagrams script in the JSON result.")] bool includePython = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _conversionService.ConvertAsync(new ConversionRequest(mermaid, format, theme), cancellationToken);
        if (!result.Success || result.DiagramBytes is null)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errors = result.Issues.Select(ToIssueDto),
                cli = ToCliDto(result.CliResult)
            }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            format = result.Format,
            contentType = result.ContentType,
            diagramBase64 = Convert.ToBase64String(result.DiagramBytes),
            pythonScript = includePython ? result.PythonScript : null,
            cli = ToCliDto(result.CliResult)
        }, JsonOptions);
    }

    private static object ToIssueDto(EligibilityIssue issue)
    {
        return new
        {
            severity = issue.Severity.ToString().ToLowerInvariant(),
            message = issue.Message
        };
    }

    private static object? ToCliDto(CliRunResult? result)
    {
        return result is null
            ? null
            : new
            {
                exitCode = result.ExitCode,
                commandLine = result.CommandLine,
                standardOutput = result.StandardOutput,
                standardError = result.StandardError
            };
    }
}
