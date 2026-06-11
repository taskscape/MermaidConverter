import importlib
import inspect
import json
import pkgutil
import sys
from pathlib import Path


def normalize(value: str) -> str:
    return "".join(ch.lower() for ch in value if ch.isalnum())


def aliases_for(category: str, class_name: str) -> list[str]:
    aliases = {class_name, class_name.replace("_", " "), normalize(class_name)}
    common = {
        "KubernetesServices": ["aks", "kubernetes service"],
        "ContainerRegistries": ["acr", "container registry"],
        "FunctionApps": ["functions", "function app", "azure functions"],
        "SQLDatabase": ["sql db", "sql database", "azure sql database"],
        "AzureCosmosDb": ["cosmos", "cosmos db", "azure cosmos db"],
        "KeyVaults": ["key vault", "kv"],
        "VirtualNetworks": ["vnet", "virtual network"],
        "FrontDoors": ["front door", "azure front door"],
        "ApplicationGateway": ["application gateway", "app gateway"],
        "PrivateLink": ["private link", "private endpoint"],
        "AzureOpenAI": ["azure openai", "openai"],
        "CognitiveSearch": ["ai search", "azure ai search", "cognitive search"],
    }
    aliases.update(common.get(class_name, []))
    aliases.add(category)
    return sorted(alias for alias in aliases if alias)


def main() -> int:
    try:
        import diagrams.azure as azure
        from diagrams import Node
    except ImportError as exc:
        print(f"Unable to import Python Diagrams: {exc}", file=sys.stderr)
        return 1

    output = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("src/MermaidToDiagrams.CLI/catalogs/azure-icons.json")
    entries = []

    for module_info in pkgutil.iter_modules(azure.__path__):
        if module_info.ispkg:
            continue

        module = importlib.import_module(f"diagrams.azure.{module_info.name}")
        for name, value in inspect.getmembers(module, inspect.isclass):
            if value.__module__ != module.__name__:
                continue
            if not issubclass(value, Node) or value is Node:
                continue

            entries.append(
                {
                    "category": module_info.name,
                    "className": name,
                    "importPath": f"diagrams.azure.{module_info.name}.{name}",
                    "aliases": aliases_for(module_info.name, name),
                }
            )

    entries.sort(key=lambda item: (item["category"], item["className"]))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(entries, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {len(entries)} Azure icon entries to {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
