## Azure Diagrams

When asked to create, validate, repair, render, or export Azure architecture diagrams from Mermaid, use the `azure-mermaid-diagrams` skill in `skills/azure-mermaid-diagrams`.

Prefer the `MermaidToDiagrams.MCP` server when available. Validate decorated Mermaid before rendering. Save source diagrams as `.mmd` and rendered outputs as `.svg` unless the user requests another format. Use REST only when MCP is unavailable or when creating scripts, batch jobs, or CI workflows.
