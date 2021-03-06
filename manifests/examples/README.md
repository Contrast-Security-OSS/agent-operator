Create new pods:

```
kubectl -n default rollout restart deployment asp-net-core
```

Exec into cluster:
```
kubectl -n default run -it --rm --image ubuntu -- bash
```

Operator service:
```
contrast-agent-operator.contrast-agent-operator.svc.cluster.local
```

Dump web hooks:
```
kubectl get MutatingWebhookConfiguration -o yaml
```

Exec into the cluster's node.
```
docker run -it --rm --privileged --pid=host ubuntu nsenter -t 1 -m -u -n -i bash

# Logs
# e.g. kube-apiserver-docker-desktop_kube-system_kube-apiserver-....
cd /var/log/containers/
```

Create the required pull secrets:
```
kubectl create secret docker-registry contrastdotnet-pull-secret --docker-server=contrastdotnet.azurecr.io --docker-username= --docker-password=""
kubectl create secret docker-registry contrastdotnet-pull-secret -n contrast-agent-operator --docker-server=contrastdotnet.azurecr.io --docker-username= --docker-password=""
```
