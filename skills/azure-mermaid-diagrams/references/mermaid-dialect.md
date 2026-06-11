# Mermaid Dialect

The converter supports a constrained Mermaid subset. Prefer simple, explicit diagrams.

## Required Metadata

Every production input should include:

```mermaid
%% m2d: strict = true %%
%% m2d: title = My Azure Architecture %%
%% m2d: convention = az_<category>_<ClassName>__<logicalId> %%
```

Recommended:

```mermaid
%% m2d: source = https://learn.microsoft.com/... %%
```

## Graph Declaration

Supported:

```mermaid
flowchart LR
graph TB
```

Directions: `LR`, `RL`, `TB`, `TD`, `BT`.

## Azure Node IDs

Use:

```text
az_<category>_<ClassName>__<logicalId>
```

Examples:

```text
az_compute_KubernetesServices__aks
az_compute_ContainerRegistries__acr
az_databases_SQLDatabase__orders
az_networking_VirtualNetworks__hub_vnet
az_security_KeyVaults__key_vault
```

The category/class should correspond to Python Diagrams Azure classes, for example:

```text
az_compute_KubernetesServices__aks
  -> diagrams.azure.compute.KubernetesServices
```

## Nodes

Use quoted labels:

```mermaid
az_compute_KubernetesServices__aks["aks-prod"]
```

Labels should be human-readable; node IDs should be machine-readable.

## Edges

Supported:

```mermaid
a --> b
a -->|label| b
a -.->|optional or async| b
a ==>|important path| b
```

Avoid reverse and bidirectional arrows:

```mermaid
a <-- b
a <--> b
```

Instead, write the edge in the intended direction.

## Clusters

Use `subgraph` for regions, VNets, subnets, resource groups, or workloads:

```mermaid
subgraph az_region_primary["Primary region"]
  az_networking_VirtualNetworks__vnet["Application VNet"]
  az_compute_KubernetesServices__aks["AKS cluster"]
end
```

## Unsupported

Avoid Mermaid features not implemented by the v1 parser:

- `classDef` styling as a source of render behavior.
- HTML labels.
- Markdown labels.
- Links/click handlers.
- Custom icons.
- Complex shapes.
- Reverse or bidirectional arrows.
- Non-Azure nodes in strict mode.
