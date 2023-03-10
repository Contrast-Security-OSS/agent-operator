# Configure the operator

All configuration of the operator is handled through the use of Kubernetes native configuration entities defined by [custom resource definitions](https://kubernetes.io/docs/concepts/extend-kubernetes/api-extension/custom-resources/) (CRDs). The CRDs are deployed with the operator and define how to interact with the operator's configuration entities.

Tooling such as VSCode's Kubernetes [extension](https://code.visualstudio.com/docs/azure/kubernetes) can aid in creating syntactically correct entities in your cluster.

> The full schema is documented in "[configuration reference](../03-configuration-reference.md)" section. This section only covers the minimal setup required and may not cover all situations.

## Minimum configuration

For a minimum setup, 3 manifests are required.

First a standard Kubernetes Secret. The Secret contains the necessary connection keys to authenticate to your Contrast server instance. The Secret must be deployed into the same namespace as the ClusterAgentConnection entity.

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: default-agent-connection-secret
  namespace: contrast-agent-operator
type: Opaque
stringData:
  apiKey: TODO
  serviceKey: TODO
  userName: TODO
```

> Finding your server keys is documented in the "[Find the agent keys](https://docs.contrastsecurity.com/en/find-the-agent-keys.html)" section.

Second, a ClusterAgentConnection configuration entity. The ClusterAgentConnection provides the default connection settings for agents within the cluster and maps to the above mentioned Secret containing connection authentication keys. For security, ClusterAgentConnection entities must be deployed into the same namespace as the operator to be used. This example assumes that the default namespace `contrast-agent-operator` hasn't been customized.

```yaml
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentConnection
metadata:
  name: default-agent-connection
  namespace: contrast-agent-operator
spec:
  template:
    spec:
      url: https://app.contrastsecurity.com/Contrast
      apiKey:
        secretName: default-agent-connection-secret
        secretKey: apiKey
      serviceKey:
        secretName: default-agent-connection-secret
        secretKey: serviceKey
      userName:
        secretName: default-agent-connection-secret
        secretKey: userName
```

Finally, a AgentInjector configuration entity. The AgentInjector selects workloads eligible for automatic injection using workload labels e.g. `metadata.labels` within the namespace in which the AgentInjector is deployed.

```yaml
apiVersion: agents.contrastsecurity.com/v1beta1
kind: AgentInjector
metadata:
  name: dotnet-hello-world
  namespace: default
spec:
  type: dotnet-core
  selector:
    labels:
      - name: app
        value: dotnet-hello-world
```

In this example manifest, the Contrast Agent Operator will automatically inject the .NET Contrast agent into workloads (e.g. Deployments, DeploymentConfigs, etc.) that have the label `app=dotnet-hello-world` in the namespace `default`.
