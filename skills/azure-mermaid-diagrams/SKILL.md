---
name: azure-mermaid-diagrams
description: Create, validate, repair, and render Azure architecture diagrams from decorated Mermaid source using the MermaidToDiagrams MCP server first, with REST or CLI fallback. Use when the user asks for Azure infrastructure diagrams, Mermaid-to-Azure conversion, Azure icon rendering, diagram validation, diagram export, or fixing Mermaid files for MermaidToDiagrams compatibility.
---

# Azure Mermaid Diagrams

Use this skill to help users create or fix Azure architecture diagrams that can be rendered by the MermaidToDiagrams product.

## Preferred Workflow

Prefer the MCP server when available. The MCP server is the agent-native interface and exposes narrow tools for validation and conversion.

1. Understand the desired Azure architecture or inspect the existing `.mmd` source.
2. Ensure the Mermaid uses the deterministic dialect in `references/mermaid-dialect.md`.
3. Call `validate_mermaid_azure_diagram` before rendering.
4. If validation fails, repair the Mermaid source and validate again.
5. Call `convert_mermaid_azure_diagram` when validation succeeds.
6. Return the rendered artifact data or saved file path, plus any warnings.
7. When working in a repository, save both the decorated `.mmd` source and rendered `.svg` or `.png`.

Read `references/mcp-usage.md` when you need exact MCP tool arguments, outputs, or local server configuration.

## Fallback Paths

Use REST when MCP is unavailable, when writing scripts or CI jobs, or when integrating with non-agent clients. Read `references/rest-api.md`.

Use the CLI directly only when working locally in this repository, debugging the converter, or when neither MCP nor REST is configured.

## Diagram Quality Checklist

Every production-quality Azure diagram should include:

- A clear title.
- Directional flows with meaningful edge labels.
- Azure service icons encoded with deterministic node IDs.
- Logical grouping with `subgraph` for regions, resource groups, VNets, subnets, workloads, or security boundaries.
- Human-readable labels that preserve user terminology.
- No secrets, tokens, connection strings, tenant IDs, subscription IDs, or private endpoint hostnames unless explicitly sanitized.
- SVG output by default for documentation; PNG for previews; PDF only when requested.

Read `references/azure-visual-style.md` when the user asks for a polished or presentation-ready diagram.

## Repair Strategy

When validation fails:

1. Keep the user’s architecture intent and labels.
2. Add missing required decorators.
3. Replace unsupported Mermaid syntax with the supported subset.
4. Convert non-deterministic node IDs to `az_<category>_<ClassName>__<logicalId>`.
5. Use `inspect-icons` or the catalog when choosing an Azure icon class.
6. Revalidate before rendering.

Read `references/troubleshooting.md` for common validation and render errors.

## Do Not

- Do not invent deployed Azure resources or claim the diagram reflects the real tenant unless the user provided infrastructure inventory.
- Do not send secrets or sensitive identifiers to MCP, REST, CLI, or generated Mermaid.
- Do not ignore validation errors from the CLI; those are the converter’s source of truth.
- Do not rely on fuzzy icon naming in production Mermaid. Use deterministic node IDs.

## Useful References

- `references/mcp-usage.md`: MCP server endpoint, tools, workflow, and response shapes.
- `references/rest-api.md`: REST fallback endpoints and examples.
- `references/mermaid-dialect.md`: supported Mermaid syntax and deterministic Azure naming.
- `references/azure-visual-style.md`: layout and visual quality rules.
- `references/troubleshooting.md`: validation, runtime, and renderer errors.
- `references/examples.md`: valid Mermaid source examples.
