# Upgrade the operator

## Operator versioning

The Contrast Agent Operator follows [semantic versioning](https://semver.org/#semantic-versioning-specification-semver).

- `MAJOR` versions may include breaking changes to the operator API. Care should be taken when upgrading between `MAJOR` versions as manifests may have changed or existing CRDs may need to be updated.
- `MINOR` versions contain new features and are fully backwards compatible and are safe to apply to an existing cluster. Optional manifest changes may be needed to use new functionality.
- `PATCH` versions contain security and bug fixes and are fully backwards compatible and are safe to apply to an existing cluster. No manifest changes are required.

Contrast publishes image tags in the following format:

```
:2
:2.1
:2.1.10
:latest
```

Where `:2` represents the latest release in the `2.X.X` semantic version branch. To simplify upgrades, prefix versions may be used based on your risk tolerance (ensure `imagePullPolicy` is set to `Always`).

> While the Contrast Agent Operator supports high availability setups using multiple replicas and leader leases, Contrast only supports deployments where all operator instances are running the same version for extended periods of time. The option `imagePullPolicy` should not be relied on to keep multiple instances on the same version. Using an operator, such as [Keel](https://keel.sh/) to facilitate safe upgrades, is recommended if automatic upgrades are desired.

## Minor and patch upgrades

Upgrading to new versions follows the same steps as installing into a fresh cluster. Executing as a cluster administrator, apply the operator manifests using `kubectl` (Kubernetes) or `oc` (OpenShift).

```bash
kubectl apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

```bash
oc apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

## Major upgrades

Major upgrades may include additional manifest changes. Deleting only the `contrast-agent-operator` namespace maintains the installed CRDs (and by extension any cluster configurations).

```bash
kubectl delete namespace contrast-agent-operator
kubectl apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

```bash
oc delete project contrast-agent-operator
oc apply -f https://github.com/Contrast-Security-OSS/agent-operator/releases/latest/download/install-prod.yaml
```

While these generic steps may work in most major upgrades, care should be taken to follow the migration steps provided in the release notes, if any, to ensure the major upgrade is successful.
