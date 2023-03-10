# Uninstalling the operator

The Contrast Agent Operator stores all data in the Kubernetes backplane, and is designed to completely remove all modifications when removed from a cluster. To ensure that everything is cleaned up correctly, it is recommended the following steps are taken in order.

First, while running as a cluster administrator, remove all custom configuration entities from all namespaces.

```bash
kubectl delete crd agentconfigurations.agents.contrastsecurity.com
kubectl delete crd agentconnections.agents.contrastsecurity.com
kubectl delete crd agentinjectors.agents.contrastsecurity.com
kubectl delete crd clusteragentconfigurations.agents.contrastsecurity.com
kubectl delete crd clusteragentconnections.agents.contrastsecurity.com
```

```bash
oc delete crd agentconfigurations.agents.contrastsecurity.com
oc delete crd agentconnections.agents.contrastsecurity.com
oc delete crd agentinjectors.agents.contrastsecurity.com
oc delete crd clusteragentconfigurations.agents.contrastsecurity.com
oc delete crd clusteragentconnections.agents.contrastsecurity.com
```

Deleting the CRDs will delete any operator configuration entities automatically. Allow the Contrast Agent Operator to reverse any changes it has made to cluster workloads once the configuration entities have been removed.

> This may cause substantial shifting of deployed pods as Kubernetes redeploys impacted workloads, depending on how many workloads were injected by the operator. Caution is advised in larger clusters.

After the cluster settles, the operator is safe to remove.

```bash
kubectl delete -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

```bash
oc delete -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

> Errors around missing CRDs is normal if the CRDs were deleted in the first step as recommended.
