from diagrams import Diagram, Cluster, Edge
from diagrams.azure.general import Developertools as Azure_general_Developertools, Resource as Azure_general_Resource

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

with Diagram("Terraform Module Layout - Entitlement Engine", filename="C:\\Projects\\AzureMermaidConverter\\samples\\terraform-module-layout", outformat="png", show=False, direction="LR", graph_attr=graph_attr):
    nodes["az_general_Developertools__tf_root"] = Azure_general_Developertools("Terraform root - per environment")
    nodes["az_general_Resource__landing_zone"] = Azure_general_Resource("landing-zone module\\nsubscription, policy, RBAC, Log Analytics\\nshared, existing")
    nodes["az_general_Resource__connectivity"] = Azure_general_Resource("connectivity module\\nhub VNet, Front Door, App Gateway, DNS, peering\\nshared, existing")
    nodes["az_general_Resource__application"] = Azure_general_Resource("application module - entitlement-engine\\nthis project: spoke subnets, apps, data, PaaS, private endpoints")

    nodes["az_general_Developertools__tf_root"] >> Edge(color="#334155") >> nodes["az_general_Resource__landing_zone"]
    nodes["az_general_Developertools__tf_root"] >> Edge(color="#334155") >> nodes["az_general_Resource__connectivity"]
    nodes["az_general_Developertools__tf_root"] >> Edge(color="#334155") >> nodes["az_general_Resource__application"]
