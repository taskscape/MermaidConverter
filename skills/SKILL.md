---
name: azure-diagrams
description: Generate or update Azure architecture diagrams by converting Mermaid flowcharts into Azure-symbol PNGs with the AzureMermaidConverter (m2d) CLI. Use WHENEVER the user asks to create, update, regenerate, modify, add, or fix Azure diagrams / Azure architecture graphics / Azure-symbol renderings from Mermaid, or to render a Mermaid flowchart with Azure icons. Covers the required m2d node-ID dialect, catalog class lookup, the installed-package module-import pitfalls, and the validate to render to review workflow, plus how to embed the result into a Markdown HLD.
---

# Generating Azure diagrams from Mermaid (AzureMermaidConverter / m2d)

This skill captures the exact, working method used to produce the Azure-symbol renderings in the Entitlement Engine HLD. Follow it whenever asked to create or modify Azure diagrams.

## 1. Scope — what can and cannot be converted

The converter renders **`flowchart` / `graph` diagrams whose nodes are Azure resources**. It does **not** support:

- `sequenceDiagram`, `erDiagram`, `classDiagram`, `stateDiagram`, `gantt` — behavioural/relational diagrams have no Azure-symbol equivalent. Leave these as Mermaid.
- Generic process/structure flowcharts with no Azure resources (e.g. phase-migration P1→P2→P3, code-module structure). Not worth converting.

If asked to "convert every diagram", convert only the Azure-architecture flowcharts and state plainly which were skipped and why.

## 2. Prerequisites (verify first)

- CLI: `m2d.exe`. Current build path: `C:\Projects\AzureMermaidConverter\src\MermaidToDiagrams.CLI\bin\Debug\net10.0\win-x64\m2d.exe` (or `dotnet run --project C:\Projects\AzureMermaidConverter\src\MermaidToDiagrams.CLI --`).
- Rendering needs Python with `diagrams` + `graphviz` packages and native Graphviz `dot.exe`.
- **Always run `m2d doctor` and `m2d ensure-dependencies` first** to confirm Python and Graphviz resolve. Note *which* Python it uses — the icon class must exist in *that* package (see pitfall in §5).
- Full tool docs: `C:\Projects\AzureMermaidConverter\README.md` and `C:\Projects\AzureMermaidConverter\skills\azure-mermaid-diagrams\references\`.

## 3. The m2d Mermaid dialect

Every source file must start with metadata, then a flowchart with a supported direction (`LR`/`RL`/`TB`/`TD`/`BT`):

```
%% m2d: strict = true %%
%% m2d: title = <diagram title> %%
%% m2d: convention = az_<category>_<ClassName>__<logicalId> %%
flowchart TB
  az_integration_ServiceBus__sb["Service Bus (events)"]
  az_compute_FunctionApps__api["Entitlements GET API"]
  az_compute_FunctionApps__api --> az_integration_ServiceBus__sb
```

Rules:
- **Node IDs are deterministic and machine-readable**: `az_<category>_<ClassName>__<logicalId>`. `category`+`ClassName` map to `diagrams.azure.<category>.<ClassName>`.
- **Labels are quoted, human-readable**, ASCII only. Keep them **short** — long labels on side-by-side nodes overlap in Graphviz layout. Prefer `"Redis / FusionCache"` over a full sentence.
- **Edges:** `a --> b`, labelled `a -->|label| b`, dotted `a -.->|label| b`, thick `a ==>|label| b`.
- **Subgraphs → clusters:** `subgraph id["Label"] ... end`. The `id` need not be an Azure node; nesting is allowed (layout is Graphviz-controlled).

## 4. Rewriting normal Mermaid into the dialect

When converting an existing HLD Mermaid flowchart, transform:

- **Shapes → plain `["..."]`.** Cylinders `[( )]`, stadium `([ ])`, rhombus etc. are not supported; use `["Label"]`.
- **Bidirectional `a <--> b` → two directed edges** (`a --> b` and, only if needed, `b --> a`). Reverse arrows `<--` are rejected — rewrite in the intended direction.
- **Chained edges `a --> b --> c` → split** into `a --> b` and `b --> c`.
- **Non-Azure elements → generic Azure icons** (there is no Dynamics/Dataverse/Stripe/SaaS icon):
  - external systems / SaaS / on-the-fly abstractions → `az_general_Resource__<id>`
  - end users / consuming apps → `az_general_Usericon__<id>`
  - Always add a caption noting these are shown as generic resources.

## 5. Resolving Azure icon classes (and the import pitfall)

Look up classes with `m2d inspect-icons --query "<term>"` or `m2d list-icons --category <cat>`.

**Pitfall (must check):** the bundled catalog may list a class under a module that the *installed* `diagrams` package does not expose, causing an `ImportError` at render time. Known cases in this estate:

- Application Insights: use `az_devops_ApplicationInsights` (the `az_monitor_ApplicationInsights` variant failed to import).
- Log Analytics: use `az_analytics_LogAnalyticsWorkspaces` (not `az_monitor_LogAnalyticsWorkspaces`).

If a render fails with `cannot import name X from diagrams.azure.<module>`, inspect the installed package (`grep '^class' <site-packages>/diagrams/azure/<module>.py`) and pick a module that actually defines the class.

**Known-good classes used in the Entitlement Engine HLD** (see `reference.md` for the full map): `az_integration_ServiceBus`, `az_integration_APIManagement`, `az_compute_FunctionApps`, `az_web_AppServices`, `az_database_DatabaseForPostgresqlServers`, `az_database_CacheForRedis`, `az_network_FrontDoors`, `az_network_ApplicationGateway`, `az_network_PrivateEndpoint`, `az_security_KeyVaults`, `az_devops_ApplicationInsights`, `az_analytics_LogAnalyticsWorkspaces`, `az_storage_DataLakeStorage`, `az_general_Usericon`, `az_general_Resource`.

## 6. Workflow

1. **Author** the `.mmd` in an `azure-diagrams/` folder next to the document (one file per diagram; keep it as the editable source).
2. **Validate:** `m2d validate <file>.mmd` — fix any dialect errors.
3. **Render:** `m2d render <file>.mmd --output <file> --format png`.
4. **Review** the PNG (open/Read it) — check for label overlap or wrong icons; shorten labels or fix classes and re-render.
5. **Embed** the PNG in the Markdown *directly beneath the original Mermaid block* (keep the Mermaid). Use a caption that links the `.mmd` source and notes any generic-resource substitutions:

```
*Azure-symbol rendering (generated from [`azure-diagrams/<file>.mmd`](./azure-diagrams/<file>.mmd) via AzureMermaidConverter).*

![<Title> — Azure rendering](./azure-diagrams/<file>.png)
```

6. When **updating** a diagram, edit the `.mmd` source, re-run validate + render, and the embedded PNG updates in place (same path).

## 7. Command cheat-sheet

```bash
EXE="C:/Projects/AzureMermaidConverter/src/MermaidToDiagrams.CLI/bin/Debug/net10.0/win-x64/m2d.exe"
"$EXE" doctor
"$EXE" ensure-dependencies
"$EXE" inspect-icons --query "service bus"
"$EXE" validate ./azure-diagrams/target-architecture.mmd
"$EXE" render  ./azure-diagrams/target-architecture.mmd --output ./azure-diagrams/target-architecture --format png
```

## Conventions summary

- One `.mmd` per diagram, stored beside the doc in `azure-diagrams/`.
- Filenames mirror the diagram (e.g. `deployment-topology.mmd` / `.png`).
- Strict mode on; ASCII, concise labels; non-Azure → `general.Resource` / `general.Usericon` with a caption note.
- Keep the Mermaid in the document; place the PNG below it.
