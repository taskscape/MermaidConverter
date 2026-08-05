from diagrams import Diagram, Cluster, Edge
from diagrams.azure.analytics import LogAnalyticsWorkspaces as Azure_analytics_LogAnalyticsWorkspaces
from diagrams.azure.compute import ContainerApps as Azure_compute_ContainerApps, FunctionApps as Azure_compute_FunctionApps
from diagrams.azure.database import CacheForRedis as Azure_database_CacheForRedis, DatabaseForPostgresqlServers as Azure_database_DatabaseForPostgresqlServers
from diagrams.azure.devops import ApplicationInsights as Azure_devops_ApplicationInsights
from diagrams.azure.general import Resource as Azure_general_Resource, Usericon as Azure_general_Usericon
from diagrams.azure.integration import APIManagement as Azure_integration_APIManagement, ServiceBus as Azure_integration_ServiceBus
from diagrams.azure.network import ApplicationGateway as Azure_network_ApplicationGateway, FrontDoors as Azure_network_FrontDoors, PrivateEndpoint as Azure_network_PrivateEndpoint
from diagrams.azure.security import KeyVaults as Azure_security_KeyVaults
from diagrams.azure.storage import DataLakeStorage as Azure_storage_DataLakeStorage
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

with Diagram("Entitlements Platform - Magnavox (UK South)", filename="C:\\Projects\\AzureMermaidConverter\\samples\\entitlements-magnavox", outformat="png", show=False, direction="TB", graph_attr=graph_attr):
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Existing estate"):
            nodes["az_compute_FunctionApps__payment"] = Azure_compute_FunctionApps("Payment Service Function")
            nodes["az_storage_DataLakeStorage__adls"] = Azure_storage_DataLakeStorage("Data Lake ADLS Gen2")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Hub VNet - shared connectivity"):
            nodes["az_network_FrontDoors__afd"] = Azure_network_FrontDoors("Azure Front Door - WAF in prod")
            nodes["az_network_ApplicationGateway__agw"] = Azure_network_ApplicationGateway("Application Gateway")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Observability"):
            nodes["az_devops_ApplicationInsights__appinsights"] = Azure_devops_ApplicationInsights("Application Insights")
            nodes["az_analytics_LogAnalyticsWorkspaces__law"] = Azure_analytics_LogAnalyticsWorkspaces("Log Analytics Workspace - shared")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Platform services - private access only"):
            nodes["az_security_KeyVaults__kv"] = Azure_security_KeyVaults("Key Vault")
            nodes["az_database_CacheForRedis__redis"] = Azure_database_CacheForRedis("Redis Cache - shared")
            nodes["az_integration_ServiceBus__sb"] = Azure_integration_ServiceBus("Service Bus")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Spoke VNet - vnet-spoke-env-uks-001"):
            with Cluster("snet-apim - internal"):
                nodes["az_integration_APIManagement__apim"] = Azure_integration_APIManagement("API Management - shared")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Spoke VNet - vnet-spoke-env-uks-001"):
            with Cluster("snet-apps - VNet integrated"):
                nodes["az_web_AppServices__entitlements_api"] = Azure_web_AppServices("Entitlements GET API")
                nodes["az_compute_ContainerApps__evaluation_service"] = Azure_compute_ContainerApps("Evaluation Service")
                nodes["az_compute_FunctionApps__ingestion"] = Azure_compute_FunctionApps("Projection Ingestion Function")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Spoke VNet - vnet-spoke-env-uks-001"):
            with Cluster("snet-data - delegated to PostgreSQL"):
                nodes["az_database_DatabaseForPostgresqlServers__pg"] = Azure_database_DatabaseForPostgresqlServers("PostgreSQL Flexible Server")
    with Cluster("Azure Subscription - Magnavox env - UK South"):
        with Cluster("Spoke VNet - vnet-spoke-env-uks-001"):
            with Cluster("snet-pe - private endpoints"):
                nodes["az_network_PrivateEndpoint__pe_kv"] = Azure_network_PrivateEndpoint("PE Key Vault")
                nodes["az_network_PrivateEndpoint__pe_redis"] = Azure_network_PrivateEndpoint("PE Redis")
                nodes["az_network_PrivateEndpoint__pe_sb"] = Azure_network_PrivateEndpoint("PE Service Bus")
    with Cluster("Internet"):
        nodes["az_general_Usericon__consumers"] = Azure_general_Usericon("Consumers\\nMobile App, DXP Web,\\nWordPress iGB Exec")
        nodes["az_general_Resource__stripe"] = Azure_general_Resource("Stripe\\npayment provider")
    with Cluster("Microsoft SaaS - UK South"):
        nodes["az_general_Resource__dataverse"] = Azure_general_Resource("Dynamics 365 Dataverse - inputs and Phase 1 record")

    nodes["az_general_Usericon__consumers"] >> Edge(color="#334155") >> nodes["az_network_FrontDoors__afd"]
    nodes["az_network_FrontDoors__afd"] >> Edge(color="#334155") >> nodes["az_network_ApplicationGateway__agw"]
    nodes["az_network_ApplicationGateway__agw"] >> Edge(color="#334155") >> nodes["az_integration_APIManagement__apim"]
    nodes["az_integration_APIManagement__apim"] >> Edge(color="#334155") >> nodes["az_web_AppServices__entitlements_api"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(color="#334155") >> nodes["az_network_PrivateEndpoint__pe_redis"]
    nodes["az_network_PrivateEndpoint__pe_redis"] >> Edge(color="#334155") >> nodes["az_database_CacheForRedis__redis"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_general_Resource__stripe"] >> Edge(color="#334155") >> nodes["az_compute_FunctionApps__payment"]
    nodes["az_compute_FunctionApps__payment"] >> Edge(color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_general_Resource__dataverse"] >> Edge(label="service endpoint", color="#334155") >> nodes["az_integration_ServiceBus__sb"]
    nodes["az_integration_ServiceBus__sb"] >> Edge(color="#334155") >> nodes["az_network_PrivateEndpoint__pe_sb"]
    nodes["az_network_PrivateEndpoint__pe_sb"] >> Edge(color="#334155") >> nodes["az_compute_FunctionApps__ingestion"]
    nodes["az_compute_FunctionApps__ingestion"] >> Edge(color="#334155") >> nodes["az_compute_ContainerApps__evaluation_service"]
    nodes["az_compute_ContainerApps__evaluation_service"] >> Edge(label="read inputs via ACL, Managed Identity", color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_compute_ContainerApps__evaluation_service"] >> Edge(color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_compute_FunctionApps__ingestion"] >> Edge(color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_compute_FunctionApps__ingestion"] >> Edge(color="#334155") >> nodes["az_network_PrivateEndpoint__pe_redis"]
    nodes["az_general_Resource__dataverse"] >> Edge(label="Synapse Link", style="dotted") >> nodes["az_storage_DataLakeStorage__adls"]
    nodes["az_storage_DataLakeStorage__adls"] >> Edge(label="reconcile and backfill", style="dotted") >> nodes["az_compute_FunctionApps__ingestion"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(style="dotted") >> nodes["az_network_PrivateEndpoint__pe_kv"]
    nodes["az_compute_ContainerApps__evaluation_service"] >> Edge(style="dotted") >> nodes["az_network_PrivateEndpoint__pe_kv"]
    nodes["az_compute_FunctionApps__ingestion"] >> Edge(style="dotted") >> nodes["az_network_PrivateEndpoint__pe_kv"]
    nodes["az_network_PrivateEndpoint__pe_kv"] >> Edge(color="#334155") >> nodes["az_security_KeyVaults__kv"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(style="dotted") >> nodes["az_devops_ApplicationInsights__appinsights"]
    nodes["az_compute_ContainerApps__evaluation_service"] >> Edge(style="dotted") >> nodes["az_devops_ApplicationInsights__appinsights"]
    nodes["az_compute_FunctionApps__ingestion"] >> Edge(style="dotted") >> nodes["az_devops_ApplicationInsights__appinsights"]
    nodes["az_integration_APIManagement__apim"] >> Edge(style="dotted") >> nodes["az_devops_ApplicationInsights__appinsights"]
    nodes["az_devops_ApplicationInsights__appinsights"] >> Edge(style="dotted") >> nodes["az_analytics_LogAnalyticsWorkspaces__law"]
