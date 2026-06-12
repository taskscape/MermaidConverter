using System.Diagnostics;

namespace MermaidToDiagrams.Core;

internal sealed class PythonDependencyManager(RuntimeLocator runtime)
{
    private static readonly RequiredPythonPackage[] RequiredPackages =
    [
        new("diagrams", "0.24.4"),
        new("graphviz", "0.20.3")
    ];

    public async Task<DependencyEnsureResult> EnsureAsync(bool installMissing, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        if (runtime.PythonExe is null)
        {
            return DependencyEnsureResult.Failed(["Python runtime was not found. Install the bundled runtime with the installer, or ensure python.exe is on PATH."]);
        }

        messages.Add($"Python: {runtime.PythonExe}");
        messages.Add(runtime.DotExe is null
            ? "Graphviz dot: not bundled/found"
            : $"Graphviz dot: {runtime.DotExe}");

        var check = await CheckAsync(cancellationToken);
        messages.AddRange(check.Messages);

        if (check.MissingPackages.Count > 0 && installMissing)
        {
            messages.Add($"Installing missing Python packages: {string.Join(", ", check.MissingPackages)}");
            var install = await InstallMissingPackagesAsync(check.MissingPackages, cancellationToken);
            messages.AddRange(install.Messages);

            if (!install.Success)
            {
                return DependencyEnsureResult.Failed(messages);
            }

            check = await CheckAsync(cancellationToken);
            messages.AddRange(check.Messages);
        }

        if (check.Success)
        {
            return DependencyEnsureResult.Successful(messages);
        }

        if (check.MissingPackages.Count > 0 && !installMissing)
        {
            messages.Add("Missing Python packages were detected. Re-run with --install to try installing them.");
        }

        return DependencyEnsureResult.Failed(messages);
    }

    private async Task<DependencyCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        const string script = """
            import importlib
            import shutil
            import sys

            required = ["diagrams", "graphviz"]
            missing = []

            for package in required:
                try:
                    module = importlib.import_module(package)
                    location = getattr(module, "__file__", "built-in")
                    print(f"ok: Python package '{package}' imported from {location}")
                except Exception as exc:
                    missing.append(package)
                    print(f"missing: Python package '{package}' ({exc})")

            dot = shutil.which("dot")
            if dot:
                print(f"ok: Graphviz dot found at {dot}")
            else:
                print("missing: Graphviz dot was not found on PATH")

            if missing:
                print("MISSING_PACKAGES:" + ",".join(missing))

            sys.exit(0 if not missing and dot else 1)
            """;

        var result = await RunPythonAsync(["-c", script], cancellationToken);
        var messages = SplitLines(result.StandardOutput)
            .Concat(SplitLines(result.StandardError))
            .ToArray();
        var missingPackages = messages
            .Where(line => line.StartsWith("MISSING_PACKAGES:", StringComparison.Ordinal))
            .SelectMany(line => line["MISSING_PACKAGES:".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DependencyCheckResult(result.ExitCode == 0, missingPackages, messages);
    }

    private async Task<DependencyEnsureResult> InstallMissingPackagesAsync(IReadOnlyList<string> missingPackages, CancellationToken cancellationToken)
    {
        var messages = new List<string>();
        var pipCheck = await RunPythonAsync(["-m", "pip", "--version"], cancellationToken);
        messages.AddRange(PrefixProcessOutput("pip", pipCheck));

        if (pipCheck.ExitCode != 0)
        {
            messages.Add("pip was not available. Trying to bootstrap pip with ensurepip.");
            var ensurePip = await RunPythonAsync(["-m", "ensurepip", "--upgrade"], cancellationToken);
            messages.AddRange(PrefixProcessOutput("ensurepip", ensurePip));

            pipCheck = await RunPythonAsync(["-m", "pip", "--version"], cancellationToken);
            messages.AddRange(PrefixProcessOutput("pip", pipCheck));
            if (pipCheck.ExitCode != 0)
            {
                messages.Add("Unable to install Python packages because pip is not available for this Python runtime.");
                return DependencyEnsureResult.Failed(messages);
            }
        }

        var installArgs = new List<string>
        {
            "-m",
            "pip",
            "install",
            "--disable-pip-version-check",
            "--no-cache-dir"
        };

        if (IsBundledPython())
        {
            var sitePackages = Path.Combine(Path.GetDirectoryName(runtime.PythonExe!)!, "Lib", "site-packages");
            Directory.CreateDirectory(sitePackages);
            installArgs.Add("--upgrade");
            installArgs.Add("--target");
            installArgs.Add(sitePackages);
            messages.Add($"Installing into bundled runtime site-packages: {sitePackages}");
        }
        else
        {
            installArgs.Add("--user");
            messages.Add("Installing into the current user's Python site-packages.");
        }

        foreach (var package in missingPackages)
        {
            var required = RequiredPackages.FirstOrDefault(p => p.Name.Equals(package, StringComparison.OrdinalIgnoreCase));
            installArgs.Add(required?.Specifier ?? package);
        }

        var install = await RunPythonAsync(installArgs, cancellationToken);
        messages.AddRange(PrefixProcessOutput("pip install", install));

        return install.ExitCode == 0
            ? DependencyEnsureResult.Successful(messages)
            : DependencyEnsureResult.Failed(messages);
    }

    private bool IsBundledPython()
    {
        if (runtime.PythonExe is null)
        {
            return false;
        }

        var pythonRoot = Path.GetFullPath(Path.GetDirectoryName(runtime.PythonExe)!);
        var bundledRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "runtime", "python"));
        return string.Equals(pythonRoot, bundledRoot, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProcessRunResult> RunPythonAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = runtime.PythonExe!,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        var graphvizBin = Path.GetDirectoryName(runtime.DotExe);
        if (!string.IsNullOrWhiteSpace(graphvizBin))
        {
            var currentPath = startInfo.Environment.TryGetValue("PATH", out var value)
                ? value
                : Environment.GetEnvironmentVariable("PATH") ?? "";
            startInfo.Environment["PATH"] = graphvizBin + Path.PathSeparator + currentPath;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start Python runtime.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new ProcessRunResult(process.ExitCode, stdout, stderr);
    }

    private static IEnumerable<string> PrefixProcessOutput(string label, ProcessRunResult result)
    {
        foreach (var line in SplitLines(result.StandardOutput))
        {
            yield return $"{label}: {line}";
        }

        foreach (var line in SplitLines(result.StandardError))
        {
            yield return $"{label}: {line}";
        }

        yield return $"{label}: exit code {result.ExitCode}";
    }

    private static IReadOnlyList<string> SplitLines(string value)
    {
        return value.Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

internal sealed record DependencyEnsureResult(bool Success, IReadOnlyList<string> Messages)
{
    public static DependencyEnsureResult Successful(IReadOnlyList<string> messages) => new(true, messages);

    public static DependencyEnsureResult Failed(IReadOnlyList<string> messages) => new(false, messages);
}

internal sealed record DependencyCheckResult(bool Success, IReadOnlyList<string> MissingPackages, IReadOnlyList<string> Messages);

internal sealed record RequiredPythonPackage(string Name, string Version)
{
    public string Specifier => $"{Name}=={Version}";
}

internal sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);
