using System.Text.Json;

namespace MermaidToDiagrams.Core;

internal sealed class AzureIconCatalog
{
    public IReadOnlyList<AzureIcon> Entries { get; }
    private readonly Dictionary<string, AzureIcon> _byKey;
    private readonly Dictionary<string, AzureIcon> _byAlias;

    private AzureIconCatalog(IReadOnlyList<AzureIcon> entries)
    {
        Entries = entries;
        _byKey = entries.ToDictionary(e => $"{e.Category}.{e.ClassName}", StringComparer.OrdinalIgnoreCase);
        _byAlias = new Dictionary<string, AzureIcon>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            foreach (var alias in entry.Aliases)
            {
                _byAlias.TryAdd(Normalize(alias), entry);
            }
        }
    }

    public static AzureIconCatalog LoadDefault()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "catalogs", "azure-icons.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "src", "MermaidToDiagrams.CLI", "catalogs", "azure-icons.json");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Azure icon catalog was not found.", path);
        }

        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<CatalogEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        return new AzureIconCatalog(entries
            .Select(e => new AzureIcon(e.Category, e.ClassName, e.ImportPath, e.Aliases ?? []))
            .ToArray());
    }

    public AzureIcon? Find(string category, string className)
    {
        _byKey.TryGetValue($"{category}.{className}", out var icon);
        return icon;
    }

    public AzureIcon? FindByAlias(string value)
    {
        _byAlias.TryGetValue(Normalize(value), out var icon);
        return icon;
    }

    public IEnumerable<AzureIcon> Search(string query)
    {
        var normalized = Normalize(query);
        return Entries
            .Select(e => new
            {
                Icon = e,
                Score =
                    Normalize(e.ClassName).Contains(normalized, StringComparison.Ordinal) ? 0 :
                    Normalize(e.Category).Contains(normalized, StringComparison.Ordinal) ? 1 :
                    e.Aliases.Any(a => Normalize(a).Contains(normalized, StringComparison.Ordinal)) ? 2 :
                    99
            })
            .Where(x => x.Score < 99)
            .OrderBy(x => x.Score)
            .ThenBy(x => x.Icon.Category)
            .ThenBy(x => x.Icon.ClassName)
            .Select(x => x.Icon);
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private sealed class CatalogEntry
    {
        public string Category { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ImportPath { get; set; } = "";
        public List<string>? Aliases { get; set; }
    }
}
