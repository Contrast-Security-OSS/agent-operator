# Install the operator

Contrast provides a single-file installation YAML that can be directly applied to a cluster and provides reasonable defaults. Additional modifications may be desired based on your specific circumstances.

Executing as a cluster administrator, apply the operator manifests using `kubectl` (Kubernetes) or `oc` (OpenShift).

```bash
kubectl apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

```bash
oc apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

The manifests:
- Create the `contrast-agent-operator` namespace.
- Install the operator Deployment workload.
- Install the required Custom Resource Definitions.
- Configure RBAC with the minimum necessary permissions.
- Register the operator for admission webhooks.

> It is possible to install into a namespace other than the default `contrast-agent-operator`, although modifications to the deployment manifests will be required.

After applying the operator manifests, wait for the cluster to converge.

```bash
kubectl -n contrast-agent-operator wait pod --for=condition=ready --selector=app.kubernetes.io/name=operator,app.kubernetes.io/part-of=contrast-agent-operator --timeout=30s
```

```bash
oc -n contrast-agent-operator wait pod --for=condition=ready --selector=app.kubernetes.io/name=operator,app.kubernetes.io/part-of=contrast-agent-operator --timeout=30s
```

When the wait command succeeds the operator is ready to be [configured](./02-configuration.md).
