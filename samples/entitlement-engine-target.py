from diagrams import Diagram, Cluster, Edge
from diagrams.azure.compute import FunctionApps as Azure_compute_FunctionApps
from diagrams.azure.database import CacheForRedis as Azure_database_CacheForRedis, DatabaseForPostgresqlServers as Azure_database_DatabaseForPostgresqlServers
from diagrams.azure.general import Developertools as Azure_general_Developertools, Resource as Azure_general_Resource, Usericon as Azure_general_Usericon
from diagrams.azure.integration import APIManagement as Azure_integration_APIManagement, ServiceBus as Azure_integration_ServiceBus
from diagrams.azure.web import AppServices as Azure_web_AppServices

graph_attr = {
    "bgcolor": "transparent",
    "fontname": "Segoe UI",
    "fontcolor": "#0f172a",
    "pad": "0.45",
    "nodesep": "0.65",
    "ranksep": "0.9",
    "splines": "ortho",
}

nodes = {}

with Diagram("Entitlement Engine - Target Architecture (Azure \u002B Dataverse)", filename="C:\\Projects\\AzureMermaidConverter\\samples\\entitlement-engine-target2", outformat="png", show=False, direction="TB", graph_attr=graph_attr):
    nodes["az_integration_ServiceBus__entitlement_events"] = Azure_integration_ServiceBus("Azure Service Bus\\nentitlement events")
    nodes["az_general_Usericon__consumers"] = Azure_general_Usericon("Mobile App, DXP,\\nWordPress iGB Exec")
    with Cluster("Consumption"):
        nodes["az_integration_APIManagement__apim"] = Azure_integration_APIManagement("APIM")
        nodes["az_web_AppServices__entitlements_api"] = Azure_web_AppServices("Entitlements GET API")
    with Cluster("Dynamics 365 Dataverse"):
        nodes["az_general_Resource__dataverse"] = Azure_general_Resource("Dataverse")
        nodes["az_general_Resource__service_endpoint"] = Azure_general_Resource("Service endpoint on\\nentitlement-relevant tables")
    with Cluster("Entitlement Engine - Azure Functions, VNet integrated"):
        nodes["az_compute_FunctionApps__evaluation_service"] = Azure_compute_FunctionApps("Evaluation Service\\nre-implements plug-in rules")
        nodes["az_general_Developertools__anti_corruption_layer"] = Azure_general_Developertools("Anti-Corruption Layer\\nDataverse SDK")
        nodes["az_web_AppServices__management_api"] = Azure_web_AppServices("Management API and UI\\nPhase 2 plus")
    with Cluster("Input domains - mastered in Dataverse"):
        nodes["az_compute_FunctionApps__payment_service"] = Azure_compute_FunctionApps("Payment Service\\nStripe to Dataverse")
        nodes["az_general_Resource__external_feeds"] = Azure_general_Resource("LiveBuzz, Veriff,\\nSalesforce feeds")
    with Cluster("Read and record store"):
        nodes["az_database_DatabaseForPostgresqlServers__pg"] = Azure_database_DatabaseForPostgresqlServers("PostgreSQL\\nFlexible Server")
        nodes["az_database_CacheForRedis__cache"] = Azure_database_CacheForRedis("Redis and FusionCache")

    nodes["az_compute_FunctionApps__payment_service"] >> Edge(color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_general_Resource__external_feeds"] >> Edge(color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_general_Resource__dataverse"] >> Edge(color="#334155") >> nodes["az_general_Resource__service_endpoint"]
    nodes["az_general_Resource__service_endpoint"] >> Edge(color="#334155") >> nodes["az_integration_ServiceBus__entitlement_events"]
    nodes["az_integration_ServiceBus__entitlement_events"] >> Edge(color="#334155") >> nodes["az_compute_FunctionApps__evaluation_service"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(color="#334155") >> nodes["az_general_Developertools__anti_corruption_layer"]
    nodes["az_general_Developertools__anti_corruption_layer"] >> Edge(label="reads", style="dotted") >> nodes["az_general_Resource__dataverse"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(color="#334155") >> nodes["az_database_CacheForRedis__cache"]
    nodes["az_integration_APIManagement__apim"] >> Edge(color="#334155") >> nodes["az_web_AppServices__entitlements_api"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(color="#334155") >> nodes["az_database_CacheForRedis__cache"]
    nodes["az_general_Usericon__consumers"] >> Edge(color="#334155") >> nodes["az_integration_APIManagement__apim"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(label="parity dual-write Phase 1 and 2", style="dotted") >> nodes["az_general_Resource__dataverse"]
