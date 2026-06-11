namespace MermaidToDiagrams.CLI;

internal sealed class CommandOptions
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _arguments = [];

    private CommandOptions()
    {
    }

    public static CommandOptions Parse(string[] args)
    {
        var parsed = new CommandOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("-", StringComparison.Ordinal))
            {
                parsed._arguments.Add(arg);
                continue;
            }

            var equals = arg.IndexOf('=');
            if (equals > 0)
            {
                parsed._options[arg[..equals]] = arg[(equals + 1)..];
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
            {
                parsed._options[arg] = args[++i];
            }
            else
            {
                parsed._options[arg] = null;
            }
        }

        return parsed;
    }

    public bool Has(string name) => _options.ContainsKey(name);

    public string? Get(params string[] names)
    {
        foreach (var name in names)
        {
            if (_options.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        return null;
    }

    public string RequiredArgument(string message)
    {
        return OptionalArgument() ?? throw new CliException(message);
    }

    public string? OptionalArgument() => _arguments.Count > 0 ? _arguments[0] : null;
}
