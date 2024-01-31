# Examples: Custom Registry

The Agent Operator support custom registries in the case where DockerHub is either inaccessible (air gapped) or where local mirrors are desireable. To support a custom registry, two modifications are required when using the standard manifests.

### Step 1

Set the `agent-operator` image to your custom registry from the default DockerHub repository (`contrast/agent-operator`). If using [Kustomize](https://kustomize.io), the `image` patch can be used, see [`./kustomization.yaml`](./kustomization.yaml).

### Step 2

Set the `CONTRAST_DEFAULT_REGISTRY` environment variable to your custom image repository (defaults to `contrast`).

> For example, if the imported images are in the form of `contrastdotnet.azurecr.io/agent-operator/agent-dotnet-core`, then `CONTRAST_DEFAULT_REGISTRY=contrastdotnet.azurecr.io/agent-operator` should be set.

Make sure to import all images built by [agent-operator-images](https://github.com/Contrast-Security-OSS/agent-operator-images). The repository names much match the following layout:

```
<CONTRAST_DEFAULT_REGISTRY>/agent-dotnet-core
<CONTRAST_DEFAULT_REGISTRY>/agent-java
<CONTRAST_DEFAULT_REGISTRY>/agent-nodejs
<CONTRAST_DEFAULT_REGISTRY>/agent-php
<CONTRAST_DEFAULT_REGISTRY>/agent-python
```

If using [Kustomize](https://kustomize.io), the `patchesStrategicMerge` patch can be used, see [`./kustomization.yaml`](./kustomization.yaml).

> Note that this assumes your default cluster pull secrets allow pulling from this custom registry (or anonymous pull is enabled).

> In the future the operator may reach out to this registry to query for version/digest information. To support such a feature, it is recommended that the custom registry support at least V2 of the Docker Registry API. Network policies should also allow the operator to reach out to the registry service.
