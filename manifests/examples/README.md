Create new pods:

```
kubectl -n default rollout restart deployment asp-net-core
```

Exec into cluster:
```
kubectl -n default run -it --rm --image ubuntu -- bash
```

```
contrast-agent-operator.contrast-agent-operator.svc.cluster.local
```


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
