using System.Reflection;
using MermaidToDiagrams.Core;

namespace MermaidToDiagrams.CLI;

internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        try
        {
            var command = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();

            return command switch
            {
                "render" => await RenderAsync(rest),
                "validate" => await ValidateAsync(rest),
                "emit-python" => await EmitPythonAsync(rest),
                "list-icons" => ListIcons(rest),
                "inspect-icons" => InspectIcons(rest),
                "doctor" => await DoctorAsync(rest),
                "--version" or "version" => PrintVersion(),
                _ => Fail($"Unknown command '{args[0]}'. Run 'm2d --help'.")
            };
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RenderAsync(string[] args)
    {
        var options = CommandOptions.Parse(args);
        var input = options.RequiredArgument("render requires an input .mmd file.");
        var output = options.Get("--output", "-o") ?? Path.ChangeExtension(input, null) ?? "diagram";
        var format = options.Get("--format", "-f") ?? "png";
        var theme = options.Get("--theme", "-t") ?? "azure-modern";
        var strict = options.Has("--strict") || !options.Has("--no-strict");
        var emitPython = options.Has("--emit-python");
        var keepTemp = options.Has("--keep-temp");

        var result = await ConvertAsync(input, output, format, theme, strict);
        PrintDiagnostics(result.Diagnostics);

        if (!result.Success)
        {
            return 2;
        }

        if (emitPython)
        {
            var pythonPath = Path.ChangeExtension(output, ".py");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pythonPath))!);
            await File.WriteAllTextAsync(pythonPath, result.PythonScript);
            Console.WriteLine($"Wrote Python script: {pythonPath}");
        }

        var renderer = new PythonRenderer(RuntimeLocator.FromAppContext());
        var renderResult = await renderer.RenderAsync(result.PythonScript, output, keepTemp);
        if (!renderResult.Success)
        {
            Console.Error.WriteLine(renderResult.Error);
            return 3;
        }

        Console.WriteLine($"Rendered: {Path.ChangeExtension(output, format)}");
        return 0;
    }

    private static async Task<int> ValidateAsync(string[] args)
    {
        var options = CommandOptions.Parse(args);
        var input = options.RequiredArgument("validate requires an input .mmd file.");
        var result = await ConvertAsync(input, Path.ChangeExtension(input, null) ?? "diagram", "png", "azure-modern", strict: true);
        PrintDiagnostics(result.Diagnostics);
        Console.WriteLine(result.Success ? "Validation succeeded." : "Validation failed.");
        return result.Success ? 0 : 2;
    }

    private static async Task<int> EmitPythonAsync(string[] args)
    {
        var options = CommandOptions.Parse(args);
        var input = options.RequiredArgument("emit-python requires an input .mmd file.");
        var output = options.Get("--output", "-o") ?? Path.ChangeExtension(input, ".py") ?? "diagram.py";
        var format = options.Get("--format", "-f") ?? "png";
        var theme = options.Get("--theme", "-t") ?? "azure-modern";
        var result = await ConvertAsync(input, Path.ChangeExtension(output, null) ?? "diagram", format, theme, strict: true);
        PrintDiagnostics(result.Diagnostics);

        if (!result.Success)
        {
            return 2;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        await File.WriteAllTextAsync(output, result.PythonScript);
        Console.WriteLine($"Wrote Python script: {output}");
        return 0;
    }

    private static int ListIcons(string[] args)
    {
        var options = CommandOptions.Parse(args);
        var category = options.Get("--category", "-c");
        var catalog = AzureIconCatalog.LoadDefault();
        var icons = catalog.Entries
            .Where(i => category is null || i.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Category)
            .ThenBy(i => i.ClassName);

        foreach (var icon in icons)
        {
            Console.WriteLine($"{icon.Category}.{icon.ClassName} -> az_{icon.Category}_{icon.ClassName}__name");
        }

        return 0;
    }

    private static int InspectIcons(string[] args)
    {
        var options = CommandOptions.Parse(args);
        var query = options.Get("--query", "-q") ?? options.OptionalArgument() ?? "";
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new CliException("inspect-icons requires --query or a positional query.");
        }

        var catalog = AzureIconCatalog.LoadDefault();
        var matches = catalog.Search(query).Take(25).ToArray();
        if (matches.Length == 0)
        {
            Console.WriteLine("No matching icons found.");
            return 1;
        }

        foreach (var icon in matches)
        {
            Console.WriteLine($"{icon.ImportPath}");
            Console.WriteLine($"  node id: az_{icon.Category}_{icon.ClassName}__logical_name");
            if (icon.Aliases.Count > 0)
            {
                Console.WriteLine($"  aliases: {string.Join(", ", icon.Aliases)}");
            }
        }

        return 0;
    }

    private static async Task<int> DoctorAsync(string[] args)
    {
        var quiet = args.Contains("--quiet");
        var runtime = RuntimeLocator.FromAppContext();
        var catalog = AzureIconCatalog.LoadDefault();

        if (!quiet)
        {
            Console.WriteLine($"m2d version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine($".NET: {Environment.Version}");
            Console.WriteLine($"Catalog entries: {catalog.Entries.Count}");
            Console.WriteLine($"Python: {runtime.PythonExe ?? "not bundled/found"}");
            Console.WriteLine($"Graphviz dot: {runtime.DotExe ?? "not bundled/found"}");
        }

        if (runtime.PythonExe is null)
        {
            Console.WriteLine("Runtime warning: bundled Python was not found. 'validate' and 'emit-python' still work; 'render' requires Python Diagrams and Graphviz.");
        }

        await Task.CompletedTask;
        return 0;
    }

    private static async Task<ConversionResult> ConvertAsync(string input, string output, string format, string theme, bool strict)
    {
        if (!File.Exists(input))
        {
            throw new CliException($"Input file not found: {input}");
        }

        var source = await File.ReadAllTextAsync(input);
        var parser = new MermaidParser();
        var diagram = parser.Parse(source);
        var catalog = AzureIconCatalog.LoadDefault();
        var resolver = new AzureTypeResolver(catalog);
        var resolved = resolver.Resolve(diagram, strict);
        var diagnostics = diagram.Diagnostics.Concat(resolved.Diagnostics).ToArray();

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return ConversionResult.Failed(diagnostics);
        }

        var emitter = new PythonDiagramsEmitter();
        var python = emitter.Emit(resolved.Diagram, new EmitOptions(output, format, theme));
        return ConversionResult.Successful(python, diagnostics);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        Mermaid To Diagrams (m2d)

        Usage:
          m2d validate <input.mmd>
          m2d emit-python <input.mmd> --output <script.py> [--format png] [--theme azure-modern]
          m2d render <input.mmd> --output <output-base> [--format png|svg|pdf] [--emit-python]
          m2d list-icons [--category compute]
          m2d inspect-icons --query aks
          m2d doctor

        Mermaid Azure node IDs must use:
          az_<category>_<ClassName>__<logicalId>
        """);
    }

    private static int PrintVersion()
    {
        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0");
        return 0;
    }

    private static void PrintDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var stream = diagnostic.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out;
            stream.WriteLine($"{diagnostic.Severity.ToString().ToLowerInvariant()}: {diagnostic.Message}");
        }
    }

    private static bool IsHelp(string value) => value is "--help" or "-h" or "help";

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        return 1;
    }
}

internal sealed class CliException(string message, int exitCode = 1) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
