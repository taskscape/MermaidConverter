from diagrams import Diagram, Cluster, Edge
from diagrams.azure.compute import FunctionApps as Azure_compute_FunctionApps
from diagrams.azure.database import CacheForRedis as Azure_database_CacheForRedis, DatabaseForPostgresqlServers as Azure_database_DatabaseForPostgresqlServers
from diagrams.azure.general import Resource as Azure_general_Resource
from diagrams.azure.integration import ServiceBus as Azure_integration_ServiceBus

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

with Diagram("Entitlement Evaluation - Payment to Projection Flow", filename="C:\\Projects\\AzureMermaidConverter\\samples\\entitlement-evaluation-sequence", outformat="png", show=False, direction="LR", graph_attr=graph_attr):
    nodes["az_general_Resource__stripe"] = Azure_general_Resource("Stripe")
    nodes["az_compute_FunctionApps__payment_service"] = Azure_compute_FunctionApps("Payment Service")
    nodes["az_general_Resource__dataverse"] = Azure_general_Resource("Dataverse")
    nodes["az_integration_ServiceBus__service_bus"] = Azure_integration_ServiceBus("Service Bus")
    nodes["az_compute_FunctionApps__evaluation_service"] = Azure_compute_FunctionApps("Evaluation Service")
    nodes["az_database_DatabaseForPostgresqlServers__pg"] = Azure_database_DatabaseForPostgresqlServers("PostgreSQL")
    nodes["az_database_CacheForRedis__cache"] = Azure_database_CacheForRedis("Redis and FusionCache")

    nodes["az_general_Resource__stripe"] >> Edge(label="1. invoice.paid or subscription.updated", color="#334155") >> nodes["az_compute_FunctionApps__payment_service"]
    nodes["az_compute_FunctionApps__payment_service"] >> Edge(label="2. write Order and Payment Schedule", color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_general_Resource__dataverse"] >> Edge(label="3. change event via service endpoint, for contactId and sector", color="#334155") >> nodes["az_integration_ServiceBus__service_bus"]
    nodes["az_integration_ServiceBus__service_bus"] >> Edge(label="4. deliver", color="#334155") >> nodes["az_compute_FunctionApps__evaluation_service"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(label="5. read Order, Product Entitlements, Verification, Payment Schedule via ACL", color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(label="6. validate and resolve, positive-only, failures resolved pre-write", color="#334155") >> nodes["az_compute_FunctionApps__evaluation_service"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(label="7. upsert resolved entitlements, Phase 2 record", color="#334155") >> nodes["az_general_Resource__dataverse"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(label="8. upsert projection, Phase 3 record", color="#334155") >> nodes["az_database_DatabaseForPostgresqlServers__pg"]
    nodes["az_compute_FunctionApps__evaluation_service"] >> Edge(label="9. invalidate keys for contact", color="#334155") >> nodes["az_database_CacheForRedis__cache"]
