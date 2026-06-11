# MCP Usage

Expected server: `MermaidToDiagrams.MCP`

Default local URL:

```text
http://127.0.0.1:5110/mcp
```

Health endpoint:

```text
GET /health
```

The server uses Streamable HTTP and the official C# MCP SDK. It is stateless and tool-only.

## Tools

### `validate_mermaid_azure_diagram`

Use before rendering. It validates:

- Mermaid is non-empty.
- Mermaid has a supported `flowchart` or `graph` declaration.
- Required `m2d` decorators are present.
- Azure node IDs follow deterministic naming.
- CLI validation succeeds.

Arguments:

```json
{
  "mermaid": "%% m2d: strict = true %%\n..."
}
```

Result is MCP text content containing JSON:

```json
{
  "valid": true,
  "issues": [],
  "cli": {
    "exitCode": 0,
    "commandLine": "...",
    "standardOutput": "Validation succeeded.",
    "standardError": ""
  }
}
```

### `convert_mermaid_azure_diagram`

Use only after validation passes, unless the user explicitly asks to attempt conversion and inspect failures.

Arguments:

```json
{
  "mermaid": "%% m2d: strict = true %%\n...",
  "format": "svg",
  "theme": "azure-modern",
  "includePython": false
}
```

Supported formats: `png`, `svg`, `pdf`.

Recommended default: `svg`.

Successful result JSON:

```json
{
  "success": true,
  "format": "svg",
  "contentType": "image/svg+xml",
  "diagramBase64": "...",
  "pythonScript": null,
  "cli": {
    "exitCode": 0,
    "standardOutput": "...",
    "standardError": ""
  }
}
```

Failure result JSON:

```json
{
  "success": false,
  "errors": [
    {
      "severity": "error",
      "message": "Graphviz dot.exe was not found."
    }
  ],
  "cli": {
    "exitCode": 3,
    "standardError": "..."
  }
}
```

## Agent Workflow

1. Generate or edit Mermaid.
2. Validate with `validate_mermaid_azure_diagram`.
3. Repair any errors.
4. Convert with `convert_mermaid_azure_diagram`.
5. Decode `diagramBase64` if the user needs a file artifact.

## IIS Hosting Notes

Publish with:

```powershell
.\tools\publish-mcp-iis.ps1
```

The IIS payload is staged at:

```text
artifacts/publish/iis-mcp
```

It includes `cli/m2d.exe`. If `artifacts/runtime` exists, it is copied to `cli/runtime` so rendering can find Python and Graphviz.

Configure:

- IIS app pool identity execute permission for `cli/m2d.exe`.
- `AllowedHosts` for the exact host name.
- `Mcp:AllowedOrigins` for trusted browser/client origins.
- HTTPS and normal IIS authentication when exposed beyond localhost.
