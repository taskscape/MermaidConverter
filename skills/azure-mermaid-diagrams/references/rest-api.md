# REST API Fallback

Use REST when MCP is unavailable, when writing CI scripts, or when integrating with non-MCP clients.

Default local run:

```powershell
dotnet run --project .\src\MermaidToDiagrams.API --urls http://localhost:5088
```

Health:

```text
GET /health
```

## Endpoints

```text
POST /api/validate
POST /api/convert
POST /api/convert/base64
```

## Validate

Request:

```json
{
  "mermaid": "%% m2d: strict = true %%\n%% m2d: title = Example %%\n%% m2d: convention = az_<category>_<ClassName>__<logicalId> %%\nflowchart LR\n  az_compute_KubernetesServices__aks[\"AKS\"]"
}
```

Response:

```json
{
  "valid": true,
  "issues": [],
  "cli": {
    "exitCode": 0,
    "standardOutput": "Validation succeeded.",
    "standardError": ""
  }
}
```

## Convert As Binary

Request:

```json
{
  "mermaid": "...",
  "format": "svg",
  "theme": "azure-modern",
  "includePython": false
}
```

`POST /api/convert` returns binary content with `image/svg+xml`, `image/png`, or `application/pdf`.

## Convert As JSON

`POST /api/convert/base64` returns:

```json
{
  "format": "svg",
  "contentType": "image/svg+xml",
  "diagramBase64": "...",
  "pythonScript": null,
  "cli": {
    "exitCode": 0
  }
}
```

## Error Handling

Validation or conversion failure returns JSON with an `errors` array and captured CLI output. Surface those messages to the user instead of summarizing them away.

## IIS Publish

```powershell
.\tools\publish-api-iis.ps1
```

The publish output is:

```text
artifacts/publish/iis-api
```

The CLI is published under:

```text
artifacts/publish/iis-api/cli/m2d.exe
```
