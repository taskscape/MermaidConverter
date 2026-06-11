# Troubleshooting

## Local Validation Errors

`Missing required decorator: %% m2d: strict = true %%`

Add:

```mermaid
%% m2d: strict = true %%
```

`Missing required convention decorator`

Add:

```mermaid
%% m2d: convention = az_<category>_<ClassName>__<logicalId> %%
```

`No deterministic Azure node IDs were found`

Rename nodes to the deterministic format:

```text
az_<category>_<ClassName>__<logicalId>
```

## CLI Validation Errors

`Azure icon catalog was not found`

The CLI must run from a publish/build output that includes:

```text
catalogs/azure-icons.json
```

`Node 'x' does not use deterministic Azure naming`

Replace `x` with a deterministic Azure node ID.

`Unsupported Mermaid statement`

Simplify the Mermaid source to the supported subset in `mermaid-dialect.md`.

## Rendering Errors

`Graphviz dot.exe was not found`

Install/stage Graphviz or run:

```powershell
.\tools\build-python-runtime.ps1 -GraphvizBinDir "C:\Program Files\Graphviz"
```

`Python runtime was not found`

Publish/install the private runtime or ensure Python is on `PATH` with `diagrams` and `graphviz` packages installed.

`ModuleNotFoundError: diagrams`

Install Python dependencies into the runtime or developer environment:

```powershell
python -m pip install diagrams graphviz
```

## API/MCP Errors

`CLI unavailable`

Set one of:

```powershell
setx MERMAID_TO_DIAGRAMS_CLI_PATH "C:\path\to\m2d.exe"
```

or configure:

```json
{
  "MermaidToDiagrams": {
    "CliPath": "C:\\path\\to\\m2d.exe"
  }
}
```

`Forbidden Origin header`

Add the trusted origin to `Mcp:AllowedOrigins`.
