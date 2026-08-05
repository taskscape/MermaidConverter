from diagrams import Diagram, Cluster, Edge
from diagrams.azure.general import Developertools as Azure_general_Developertools, Resource as Azure_general_Resource, Usericon as Azure_general_Usericon

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

with Diagram("Entitlement Evaluation - Dynamics 365 / Dataverse (UK South)", filename="C:\\Projects\\AzureMermaidConverter\\samples\\entitlement-evaluation-dataverse", outformat="png", show=False, direction="LR", graph_attr=graph_attr):
    nodes["az_general_Resource__sales_order_trigger"] = Azure_general_Resource("Sales Order /\\nPayment Schedule /\\nVerification change")
    nodes["az_general_Usericon__user_apps"] = Azure_general_Usericon("User-facing apps")
    with Cluster("Dynamics 365 and Dataverse - UK South"):
        nodes["az_general_Resource__evaluation_queue"] = Azure_general_Resource("dw_entitlementevaluationqueue")
        nodes["az_general_Developertools__evaluation_plugin"] = Azure_general_Developertools("C# Evaluation Plug-in")
        nodes["az_general_Resource__master_entitlement"] = Azure_general_Resource("Master Entitlement")
        nodes["az_general_Resource__product_entitlement"] = Azure_general_Resource("Product Entitlement")
        nodes["az_general_Resource__contact_account_entitlement"] = Azure_general_Resource("Contact / Account Entitlement")

    nodes["az_general_Resource__sales_order_trigger"] >> Edge(color="#334155") >> nodes["az_general_Resource__evaluation_queue"]
    nodes["az_general_Resource__evaluation_queue"] >> Edge(color="#334155") >> nodes["az_general_Developertools__evaluation_plugin"]
    nodes["az_general_Resource__master_entitlement"] >> Edge(color="#334155") >> nodes["az_general_Resource__product_entitlement"]
    nodes["az_general_Resource__product_entitlement"] >> Edge(color="#334155") >> nodes["az_general_Developertools__evaluation_plugin"]
    nodes["az_general_Developertools__evaluation_plugin"] >> Edge(color="#334155") >> nodes["az_general_Resource__contact_account_entitlement"]
    nodes["az_general_Usericon__user_apps"] >> Edge(label="direct Dataverse query", color="#334155") >> nodes["az_general_Resource__contact_account_entitlement"]
