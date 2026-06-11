using System.Text;

namespace MermaidToDiagrams.Shared;

public sealed class MermaidConversionService
{
    private readonly EligibilityChecker _eligibilityChecker;
    private readonly CliRunner _cliRunner;

    public MermaidConversionService(EligibilityChecker eligibilityChecker, CliRunner cliRunner)
    {
        _eligibilityChecker = eligibilityChecker;
        _cliRunner = cliRunner;
    }

    public async Task<ValidationResult> ValidateAsync(string? mermaid, CancellationToken cancellationToken = default)
    {
        var issues = _eligibilityChecker.Check(mermaid);
        if (issues.Any(i => i.Severity == EligibilitySeverity.Error))
        {
            return new ValidationResult(false, issues, null);
        }

        var inputPath = await WriteTempInputAsync(mermaid!, cancellationToken);
        CliRunResult cliResult;
        try
        {
            cliResult = await _cliRunner.RunAsync(["validate", inputPath], cancellationToken);
        }
        finally
        {
            DeleteFileQuietly(inputPath);
        }

        var cliErrors = ExtractCliErrors(cliResult);
        var allIssues = issues.Concat(cliErrors).ToArray();

        return new ValidationResult(cliResult.ExitCode == 0, allIssues, cliResult);
    }

    public async Task<ConversionResult> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request.Mermaid, cancellationToken);
        if (!validation.Success)
        {
            return ConversionResult.Failed(validation.Issues, validation.CliResult);
        }

        var inputPath = await WriteTempInputAsync(request.Mermaid!, cancellationToken);
        var outputDir = Path.Combine(Path.GetTempPath(), "m2d-api", Guid.NewGuid().ToString("N"));
        var outputBase = Path.Combine(outputDir, "diagram");
        Directory.CreateDirectory(outputDir);

        try
        {
            var format = NormalizeFormat(request.Format);
            var theme = string.IsNullOrWhiteSpace(request.Theme) ? "azure-modern" : request.Theme.Trim();
            var cliArgs = new[]
            {
                "render",
                inputPath,
                "--output",
                outputBase,
                "--format",
                format,
                "--theme",
                theme,
                "--emit-python",
                "--strict"
            };

            var cliResult = await _cliRunner.RunAsync(cliArgs, cancellationToken);
            if (cliResult.ExitCode != 0)
            {
                return ConversionResult.Failed(ExtractCliErrors(cliResult), cliResult);
            }

            var outputPath = Path.ChangeExtension(outputBase, format);
            if (!File.Exists(outputPath))
            {
                return ConversionResult.Failed([EligibilityIssue.Error($"CLI completed but expected output file was not found: {outputPath}")], cliResult);
            }

            var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
            var pythonPath = Path.ChangeExtension(outputBase, ".py");
            var python = File.Exists(pythonPath) ? await File.ReadAllTextAsync(pythonPath, cancellationToken) : null;
            return ConversionResult.Successful(bytes, format, GetContentType(format), python, cliResult);
        }
        finally
        {
            DeleteFileQuietly(inputPath);
            DeleteDirectoryQuietly(outputDir);
        }
    }

    private static async Task<string> WriteTempInputAsync(string mermaid, CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "m2d-api");
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, $"input-{Guid.NewGuid():N}.mmd");
        await File.WriteAllTextAsync(path, mermaid, Encoding.UTF8, cancellationToken);
        return path;
    }

    private static string NormalizeFormat(string? format)
    {
        var value = string.IsNullOrWhiteSpace(format) ? "png" : format.Trim().ToLowerInvariant();
        return value is "png" or "svg" or "pdf" ? value : "png";
    }

    private static string GetContentType(string format)
    {
        return format switch
        {
            "svg" => "image/svg+xml",
            "pdf" => "application/pdf",
            _ => "image/png"
        };
    }

    private static void DeleteFileQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }

    private static void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }

    private static IReadOnlyList<EligibilityIssue> ExtractCliErrors(CliRunResult cliResult)
    {
        if (cliResult.ExitCode == 0)
        {
            return [];
        }

        var lines = (cliResult.StandardError + Environment.NewLine + cliResult.StandardOutput)
            .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines.Length == 0
            ? [EligibilityIssue.Error($"CLI failed with exit code {cliResult.ExitCode}.")]
            : lines.Select(line => EligibilityIssue.Error(line)).ToArray();
    }
}

public sealed record ConversionRequest(string? Mermaid, string? Format = "png", string? Theme = "azure-modern");

public sealed record ValidationResult(bool Success, IReadOnlyList<EligibilityIssue> Issues, CliRunResult? CliResult);

public sealed record ConversionResult(
    bool Success,
    IReadOnlyList<EligibilityIssue> Issues,
    byte[]? DiagramBytes,
    string? Format,
    string? ContentType,
    string? PythonScript,
    CliRunResult? CliResult)
{
    public static ConversionResult Successful(byte[] diagramBytes, string format, string contentType, string? pythonScript, CliRunResult cliResult)
    {
        return new ConversionResult(true, [], diagramBytes, format, contentType, pythonScript, cliResult);
    }

    public static ConversionResult Failed(IReadOnlyList<EligibilityIssue> issues, CliRunResult? cliResult)
    {
        return new ConversionResult(false, issues, null, null, null, null, cliResult);
    }
}
