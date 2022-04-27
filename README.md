# agent-operator

A K8s operator to inject agents into existing K8s workloads.

Managed by the .NET team.

## Development

As this is an operator, local development requires the interactions of a K8s cluster.

### Development with Docker Desktop

The easiest method to develop "pull" features (features that does not require the back plane to communicate with our app) is using Docker Desktop in K8s mode.

- Ensure Docker Desktop is installed (Enable WSL integration is recommended, for the lightweight containers).
- Ensure Docker Desktop is in Linux Containers mode.
- In Docker Desktop Settings, ensure that "Kubernetes > Enable Kubernetes" is enabled.
- Under the host (not WSL), ensure `cluster-info` looks like:

```
# kubectl cluster-info
Kubernetes control plane is running at https://kubernetes.docker.internal:6443
CoreDNS is running at https://kubernetes.docker.internal:6443/api/v1/namespaces/kube-system/services/kube-dns:dns/proxy
```

With Docker Desktop executing locally, the operator should automatically connect to the local back plane using your local `kubeconfig`.

## Data Flow

Data flow is unidirectional when possible.

![Data Flow](./docs/data-flow.png)
