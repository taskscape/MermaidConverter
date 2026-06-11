using System.Diagnostics;

namespace MermaidToDiagrams.Core;

internal sealed record RuntimeLocator(string? PythonExe, string? DotExe)
{
    public static RuntimeLocator FromAppContext()
    {
        var baseDir = AppContext.BaseDirectory;
        var pythonCandidates = new[]
        {
            Path.Combine(baseDir, "runtime", "python", "python.exe"),
            Path.Combine(baseDir, "runtime", "python", "python3.exe"),
            ResolveFromPath("python.exe"),
            ResolveFromPath("python")
        };

        var dotCandidates = new[]
        {
            Path.Combine(baseDir, "runtime", "graphviz", "bin", "dot.exe"),
            ResolveFromPath("dot.exe"),
            ResolveFromPath("dot")
        };

        return new RuntimeLocator(
            pythonCandidates.FirstOrDefault(File.Exists),
            dotCandidates.FirstOrDefault(File.Exists));
    }

    private static string? ResolveFromPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(entry.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

internal sealed class PythonRenderer(RuntimeLocator runtime)
{
    public async Task<RenderResult> RenderAsync(string pythonScript, string outputBasePath, bool keepTemp)
    {
        if (runtime.PythonExe is null)
        {
            return RenderResult.Failed("Python runtime was not found. Install the bundled runtime with the installer, or ensure python.exe is on PATH with diagrams and graphviz installed.");
        }

        if (runtime.DotExe is null)
        {
            return RenderResult.Failed("Graphviz dot.exe was not found. Install the bundled runtime with the installer, or ensure Graphviz is on PATH.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "m2d", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var scriptPath = Path.Combine(tempDir, "render.py");
        await File.WriteAllTextAsync(scriptPath, pythonScript);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputBasePath))!);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = runtime.PythonExe,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add(scriptPath);

            var graphvizBin = Path.GetDirectoryName(runtime.DotExe);
            if (!string.IsNullOrWhiteSpace(graphvizBin))
            {
                var currentPath = process.StartInfo.Environment.TryGetValue("PATH", out var value) ? value : Environment.GetEnvironmentVariable("PATH") ?? "";
                process.StartInfo.Environment["PATH"] = graphvizBin + Path.PathSeparator + currentPath;
            }

            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return RenderResult.Failed($"Python renderer failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{stderr}");
            }

            return RenderResult.Successful();
        }
        finally
        {
            if (!keepTemp && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}

internal sealed record RenderResult(bool Success, string Error)
{
    public static RenderResult Successful() => new(true, "");
    public static RenderResult Failed(string error) => new(false, error);
}
