using System.Diagnostics;

namespace MermaidToDiagrams.Shared;

public sealed class CliRunner
{
    private readonly string? _configuredCliPath;

    public CliRunner(string? configuredCliPath = null)
    {
        _configuredCliPath = configuredCliPath;
    }

    public async Task<CliRunResult> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)
    {
        var cliPath = ResolveCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start MermaidToDiagrams.CLI.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CliRunResult(process.ExitCode, cliPath + " " + string.Join(" ", args.Select(QuoteArg)), stdout, stderr);
    }

    public string ResolveCliPath()
    {
        var envPath = Environment.GetEnvironmentVariable("MERMAID_TO_DIAGRAMS_CLI_PATH");
        var baseDir = AppContext.BaseDirectory;
        var configured = _configuredCliPath;
        var repoRoot = FindRepositoryRoot(baseDir);

        var candidates = new[]
        {
            configured,
            envPath,
            Path.Combine(baseDir, "m2d.exe"),
            Path.Combine(baseDir, "cli", "m2d.exe"),
            repoRoot is null ? null : Path.Combine(repoRoot, "artifacts", "publish", "win-x64", "m2d.exe"),
            repoRoot is null ? null : Path.Combine(repoRoot, "src", "MermaidToDiagrams.CLI", "bin", "Debug", "net10.0", "win-x64", "m2d.exe"),
            repoRoot is null ? null : Path.Combine(repoRoot, "src", "MermaidToDiagrams.CLI", "bin", "Release", "net10.0", "win-x64", "m2d.exe"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "MermaidToDiagrams.CLI", "bin", "Debug", "net10.0", "win-x64", "m2d.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "MermaidToDiagrams.CLI", "bin", "Release", "net10.0", "win-x64", "m2d.exe"))
        };

        var found = candidates.Where(p => !string.IsNullOrWhiteSpace(p)).FirstOrDefault(File.Exists);
        if (found is not null)
        {
            return found;
        }

        throw new FileNotFoundException("Could not find m2d.exe. Publish MermaidToDiagrams.CLI alongside the caller, place it under a cli subfolder, or set MERMAID_TO_DIAGRAMS_CLI_PATH.");
    }

    private static string? FindRepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MermaidToDiagrams.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string QuoteArg(string value)
    {
        return value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;
    }
}

public sealed record CliRunResult(int ExitCode, string CommandLine, string StandardOutput, string StandardError);
