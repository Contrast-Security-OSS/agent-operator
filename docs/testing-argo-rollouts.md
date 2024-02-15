# Testing Argo Rollouts

Follow [argo rollouts installation](https://argo-rollouts.readthedocs.io/en/stable/installation/) instructions to install the controller and kubectl plugin

Windows Note: Rename the downloaded executable from `kubectl-argo-rollouts-windows-amd64` to `kubectl-argo-rollouts` and have the folder in the `PATH`

## Actually Testing

Deploy `install-prod.yaml` from [releases](https://github.com/Contrast-Security-OSS/agent-operator/releases).

```bash
# Install the production manifests.
kubectl apply -f install-prod.yaml

# Wait for the cluster to converge.
watch kubectl -n contrast-agent-operator get pods

# Check the logs for any problems.
kubectl -n contrast-agent-operator logs deployment/contrast-agent-operator -f
```

Then we can deploy argo rollout examples

```bash
# Install the Argo Rollout only examples (e.g. Rollout).
kubectl apply -k ./manifests/examples/argo-rollouts

# Promote the rollout to finish the agent injection
kubectl argo rollouts promote dotnet-core-app

# Inspect rollout in another terminal
kubectl argo rollouts get rollout dotnet-core-app --watch
```

We can also force a change on the container to trigger a rollout

```bash
# Optional: Force a change to the container to trigger a rollout
kubectl argo rollouts set image dotnet-core-app dotnet-core-app=contrast/sample-app-aspnetcore:main

# Promote the rollout to finish
kubectl argo rollouts promote dotnet-core-app
```
