# Full example

## Before you begin

This topic provides a complete walk-through of installing the Contrast Agent Operator and injecting an example workload as a cluster administrator, using vanilla Kubernetes. To follow this example using OpenShift, the Kubernetes commands will need to be converted to their OpenShift equivalents. All commands are expected to execute within a Bash-like terminal.

You should have a basic understanding of how Kubernetes and related software work. You may need to adjust the instructions to meet your specific circumstances.

## Step 1: Install the operator

To install the operator, the operator manifests must be applied to the cluster. Contrast provides a single-file installation YAML that can be directly applied to a cluster and provides reasonable defaults. Additional modifications may be desired based on your specific circumstances, in which case, a configuration management framework, such as [Kustomize](https://kustomize.io/), is recommended.

Note that this single-file installation YAML will create and install into the `contrast-agent-operator` namespace. This namespace will be used later.

```
% kubectl apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml

namespace/contrast-agent-operator created
customresourcedefinition.apiextensions.k8s.io/agentconfigurations.agents.contrastsecurity.com created
customresourcedefinition.apiextensions.k8s.io/agentconnections.agents.contrastsecurity.com created
customresourcedefinition.apiextensions.k8s.io/agentinjectors.agents.contrastsecurity.com created
customresourcedefinition.apiextensions.k8s.io/clusteragentconfigurations.agents.contrastsecurity.com created
customresourcedefinition.apiextensions.k8s.io/clusteragentconnections.agents.contrastsecurity.com created
serviceaccount/contrast-agent-operator-service-account created
clusterrole.rbac.authorization.k8s.io/contrast-agent-operator-service-role created
clusterrolebinding.rbac.authorization.k8s.io/contrast-agent-operator-service-role-binding created
service/contrast-agent-operator created
deployment.apps/contrast-agent-operator created
poddisruptionbudget.policy/contrast-agent-operator created
mutatingwebhookconfiguration.admissionregistration.k8s.io/contrast-web-hook-configuration created
```

After waiting for cluster convergence, the operator should be ready in the `Running` status.

```bash
% kubectl -n contrast-agent-operator get pods

NAME                                      READY   STATUS    RESTARTS   AGE
contrast-agent-operator-57f5cfbf7-9svtt   1/1     Running   0          27s
contrast-agent-operator-57f5cfbf7-fp4vp   1/1     Running   0          39s
```

At this point, the operator is ready to be configured.

## Step 2: Configure the operator

The operator must first be configured before injecting cluster workloads.

Kubernetes secrets are used to store connection authentication keys. Note that the name of the Secret created in the next part is called `default-agent-connection-secret` and is created in the `contrast-agent-operator` namespace.

```bash
% kubectl -n contrast-agent-operator \
        create secret generic default-agent-connection-secret \
        --from-literal=apiKey=TODO \
        --from-literal=serviceKey=TODO \
        --from-literal=userName=TODO

secret/default-agent-connection-secret created
```

> Replace `TODO` with the equivalent values for your Contrast server instance. [Find the agent keys](https://docs.contrastsecurity.com/en/find-the-agent-keys.html) describes how to retrieve agent keys from the Contrast UI.

To complete the connection configuration, a ClusterAgentConnection is needed. Note that ClusterAgentConnection created in the next part is created in the `contrast-agent-operator` namespace and refers to the Secret's key values used above.

```bash
% kubectl apply -f - <<EOF
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
EOF

clusteragentconnection.agents.contrastsecurity.com/default-agent-connection created
```

> The name of the ClusterAgentConnection is not important and can be named anything.

At this point, the operator is configured and can inject agents into existing workloads.

## Step 3: Inject workloads

This example will focus on injecting the Contrast .NET Core Agent into the ASP.&#8203;NET Core [sample application](https://hub.docker.com/_/microsoft-dotnet-samples) using a Deployment workload.

First, deploy the ASP.&#8203;NET Core sample application to the cluster. Note that the Deployment created in the next part is created in the `default` namespace.

```bash
% kubectl apply -f - <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hello-world-app
  namespace: default
  labels:
    arbitrary-label: arbitrary-value
spec:
  selector:
    matchLabels:
      app: hello-world-app
  template:
    metadata:
      labels:
        app: hello-world-app
    spec:
      containers:
        - image: contrast/sample-app-aspnetcore:latest
          name: hello-world-app
EOF

deployment.apps/hello-world-app created
```

After waiting for cluster convergence, the deployed workload should be ready in the `Running` status.

```bash
$% kubectl -n default get pods

NAME                               READY   STATUS    RESTARTS   AGE
hello-world-app-7479d5ff96-p28zx   1/1     Running   0          19s
```

Next, the operator can be configured to inject the .NET Core agent using an AgentInjector configuration entity. Note that the AgentInjector needs to be created in the same namespace that the previous Deployment was deployed into, `default` in this case.

```bash
% kubectl apply -f - <<EOF
apiVersion: agents.contrastsecurity.com/v1beta1
kind: AgentInjector
metadata:
  name: injector-for-hello-world
  namespace: default
spec:
  type: dotnet-core
  selector:
    labels:
      - name: arbitrary-label
        value: arbitrary-value
EOF

agentinjector.agents.contrastsecurity.com/injector-for-hello-world created
```

Checking the logs of the `hello-world-app` Pod shows that the Contrast .NET Core agent is now instrumenting the application.

```bash
% kubectl -n default logs Deployment/hello-world-app

Defaulted container "hello-world-app" out of: hello-world-app, contrast-init (init)
warn: Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository[60]
      Storing keys in a directory '/root/.aspnet/DataProtection-Keys' that may not be persisted outside of the container. Protected data will be unavailable when container is destroyed.
warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
      No XML encryptor configured. Key {0ad20893-267f-4635-99b0-1ee74bccbc8b} may be persisted to storage in unencrypted form.
           ___
       _.-|   |          |\__/,|   (`\     Contrast .NET Core Agent 2.1.13.0
      [   |   |          |o o  |__ _) )
       `-.|___|        _.( T   )  `  /     Contrast UI: https://app.contrastsecurity.com
        .--'-`-.     _((_ `^--' /_<  \     Mode:        Assess & Protect
      .+|______|__.-||__)`-'(((/  (((/

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:80
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /app/
```

## Step 4: Uninstall the operator (optional)

To restore the original state of the cluster, first remove existing AgentInjectors.

```bash
% kubectl -n default delete agentinjector injector-for-hello-world

agentinjector.agents.contrastsecurity.com "injector-for-hello-world" deleted
```

After which, the operator will restore all injected workloads to their previous non-instrumented state. Once the cluster converges, the operator can be safely removed.

```bash
% kubectl delete -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml

namespace "contrast-agent-operator" deleted
customresourcedefinition.apiextensions.k8s.io "agentconfigurations.agents.contrastsecurity.com" deleted
customresourcedefinition.apiextensions.k8s.io "agentconnections.agents.contrastsecurity.com" deleted
customresourcedefinition.apiextensions.k8s.io "agentinjectors.agents.contrastsecurity.com" deleted
customresourcedefinition.apiextensions.k8s.io "clusteragentconfigurations.agents.contrastsecurity.com" deleted
customresourcedefinition.apiextensions.k8s.io "clusteragentconnections.agents.contrastsecurity.com" deleted
serviceaccount "contrast-agent-operator-service-account" deleted
clusterrole.rbac.authorization.k8s.io "contrast-agent-operator-service-role" deleted
clusterrolebinding.rbac.authorization.k8s.io "contrast-agent-operator-service-role-binding" deleted
service "contrast-agent-operator" deleted
deployment.apps "contrast-agent-operator" deleted
poddisruptionbudget.policy "contrast-agent-operator" deleted
mutatingwebhookconfiguration.admissionregistration.k8s.io "contrast-web-hook-configuration" deleted
```
