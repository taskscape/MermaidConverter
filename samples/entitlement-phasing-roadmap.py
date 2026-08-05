from diagrams import Diagram, Cluster, Edge
from diagrams.azure.general import Resource as Azure_general_Resource

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

with Diagram("Entitlement Platform - Phasing Roadmap", filename="C:\\Projects\\AzureMermaidConverter\\samples\\entitlement-phasing-roadmap", outformat="png", show=False, direction="LR", graph_attr=graph_attr):
    nodes["az_general_Resource__phase1"] = Azure_general_Resource("Phase 1\\nRead API \u002B projection\\nD365 master, mgmt in D365")
    nodes["az_general_Resource__phase2"] = Azure_general_Resource("Phase 2\\nExternal evaluation \u002B mgmt\\ndata still in Dataverse\\nplug-in retired")
    nodes["az_general_Resource__phase3"] = Azure_general_Resource("Phase 3\\nPostgreSQL = record for\\nresolved entitlements\\nDataverse tables decommissioned")

    nodes["az_general_Resource__phase1"] >> Edge(color="#334155") >> nodes["az_general_Resource__phase2"]
    nodes["az_general_Resource__phase2"] >> Edge(color="#334155") >> nodes["az_general_Resource__phase3"]
