# Azure Visual Style

Use these rules when the user asks for a beautiful or presentation-ready Azure diagram.

## Layout

- Use `flowchart LR` for most architecture diagrams.
- Use `flowchart TB` for pipelines or layered request flows.
- Group related resources with `subgraph`.
- Keep high-level ingress on the left/top and data stores on the right/bottom.
- Avoid crossing edges by grouping shared services such as Key Vault, Monitor, and Log Analytics near the consumers.

## Labels

- Use concise labels: `AKS cluster`, `Azure SQL Database`, `Key Vault`.
- Put protocols or intent on edges: `HTTPS`, `private endpoint`, `image pull`, `telemetry`, `queries`.
- Preserve user-provided names when they matter.
- Avoid long labels that will dominate the rendered icon.

## Output Format

- Use `svg` for documentation and source-controlled artifacts.
- Use `png` for quick previews.
- Use `pdf` for slide decks or documents when requested.

## Themes

Supported themes:

```text
azure-modern
azure-dark
```

Prefer `azure-modern` unless the user asks for a dark visual.

## Content Safety

Sanitize or omit:

- Secrets and keys.
- Connection strings.
- Tenant IDs.
- Subscription IDs.
- Private endpoint FQDNs.
- Internal IPs unless the user explicitly wants network-level detail.
