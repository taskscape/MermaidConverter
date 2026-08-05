from diagrams import Diagram, Cluster, Edge
from diagrams.azure.database import CacheForRedis as Azure_database_CacheForRedis, DatabaseForPostgresqlServers as Azure_database_DatabaseForPostgresqlServers
from diagrams.azure.general import Usericon as Azure_general_Usericon
from diagrams.azure.integration import APIManagement as Azure_integration_APIManagement
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

with Diagram("Entitlements Read Path - Cache-Aside (GET API)", filename="C:\\Projects\\AzureMermaidConverter\\samples\\entitlements-read-sequence", outformat="png", show=False, direction="LR", graph_attr=graph_attr):
    nodes["az_general_Usericon__client_apps"] = Azure_general_Usericon("Mobile / DXP / WordPress")
    nodes["az_integration_APIManagement__apim"] = Azure_integration_APIManagement("APIM")
    nodes["az_web_AppServices__entitlements_api"] = Azure_web_AppServices("Entitlements GET API")
    nodes["az_database_CacheForRedis__cache"] = Azure_database_CacheForRedis("Cache")
    nodes["az_database_DatabaseForPostgresqlServers__pg"] = Azure_database_DatabaseForPostgresqlServers("PostgreSQL")

    nodes["az_general_Usericon__client_apps"] >> Edge(label="1. GET entitlements for contactId, with JWT", color="#334155") >> nodes["az_integration_APIManagement__apim"]
    nodes["az_integration_APIManagement__apim"] >> Edge(label="2. forward with subscription key and policy", color="#334155") >> nodes["az_web_AppServices__entitlements_api"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(label="3. lookup", color="#334155") >> nodes["az_database_CacheForRedis__cache"]
    nodes["az_database_CacheForRedis__cache"] >> Edge(label="4. cache hit - return entitlements", style="dotted") >> nodes["az_web_AppServices__entitlements_api"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(label="5. cache miss - query resolved entitlements", color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_database_DatabaseForPostgresqlServers__pg"] >> Edge(label="6. cache miss - rows", style="dotted") >> nodes["az_web_AppServices__entitlements_api"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(label="7. cache miss - populate with TTL", color="#334155") >> nodes["az_database_CacheForRedis__cache"]
    nodes["az_web_AppServices__entitlements_api"] >> Edge(label="8. entitlement set in MembershipAccessControlItem shape", style="dotted") >> nodes["az_general_Usericon__client_apps"]
