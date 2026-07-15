# Reference — node-ID class map & gotchas

## Verified node-ID → Python Diagrams class map

These were validated by rendering the Entitlement Engine HLD diagrams against the installed `diagrams` package. `logicalId` is any short suffix you choose.

| Purpose | Node ID prefix | Renders as |
| --- | --- | --- |
| Service Bus | `az_integration_ServiceBus__<id>` | `diagrams.azure.integration.ServiceBus` |
| API Management (APIM) | `az_integration_APIManagement__<id>` | `diagrams.azure.integration.APIManagement` |
| Azure Functions / Function App | `az_compute_FunctionApps__<id>` | `diagrams.azure.compute.FunctionApps` |
| App Service (web/API/UI) | `az_web_AppServices__<id>` | `diagrams.azure.web.AppServices` |
| PostgreSQL Flexible Server | `az_database_DatabaseForPostgresqlServers__<id>` | `diagrams.azure.database.DatabaseForPostgresqlServers` |
| Cache for Redis | `az_database_CacheForRedis__<id>` | `diagrams.azure.database.CacheForRedis` |
| Front Door | `az_network_FrontDoors__<id>` | `diagrams.azure.network.FrontDoors` |
| Application Gateway | `az_network_ApplicationGateway__<id>` | `diagrams.azure.network.ApplicationGateway` |
| Private Endpoint | `az_network_PrivateEndpoint__<id>` | `diagrams.azure.network.PrivateEndpoint` |
| Key Vault | `az_security_KeyVaults__<id>` | `diagrams.azure.security.KeyVaults` |
| Application Insights | `az_devops_ApplicationInsights__<id>` | `diagrams.azure.devops.ApplicationInsights` |
| Log Analytics Workspace | `az_analytics_LogAnalyticsWorkspaces__<id>` | `diagrams.azure.analytics.LogAnalyticsWorkspaces` |
| Data Lake (ADLS Gen2) | `az_storage_DataLakeStorage__<id>` | `diagrams.azure.storage.DataLakeStorage` |
| Virtual Network | `az_networking_VirtualNetworks__<id>` | `diagrams.azure.networking.VirtualNetworks` |
| Users / consumers | `az_general_Usericon__<id>` | `diagrams.azure.general.Usericon` |
| Generic / external / SaaS | `az_general_Resource__<id>` | `diagrams.azure.general.Resource` |

## Non-Azure element mapping (no native icon)

| Element in HLD | Represent as |
| --- | --- |
| Dynamics 365 / Dataverse | `az_general_Resource` |
| Stripe / payment provider | `az_general_Resource` |
| LiveBuzz / Veriff / Salesforce feeds | `az_general_Resource` |
| Anti-Corruption Layer / SDK abstraction | `az_general_Resource` |
| Mobile / DXP / WordPress consumers | `az_general_Usericon` |

## Import-pitfall log

The catalog offered `monitor` variants for these, but the installed `diagrams` package (`diagrams.azure.monitor`) only defines `ChangeAnalysis, Logs, Metrics, Monitor`. Use the alternatives:

- `ApplicationInsights` → `az_devops_ApplicationInsights` (NOT `az_monitor_ApplicationInsights`)
- `LogAnalyticsWorkspaces` → `az_analytics_LogAnalyticsWorkspaces` (NOT `az_monitor_LogAnalyticsWorkspaces`)

Diagnose future cases with:

```bash
PY=<path-to>/site-packages
grep -oE '^class [A-Za-z0-9_]+' "$PY/diagrams/azure/<module>.py"
```

## Dialect rewrite quick list

- `[(cylinder)]`, `([stadium])`, `{rhombus}` → `["Label"]`
- `a <--> b` → `a --> b` (+ `b --> a` only if truly needed)
- `a <-- b` → `b --> a`
- `a --> b --> c` → `a --> b` and `b --> c`
- Any non-Azure node → `az_general_Resource` / `az_general_Usericon`
- Labels: ASCII, short, quoted.
