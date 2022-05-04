Install manifests for local development:

```
kubectl apply -k .\install\dev
```

A pull secret is required to pull our agent images:
```
kubectl create secret docker-registry contrastdotnet-pull-secret --docker-server=contrastdotnet.azurecr.io --docker-username= --docker-password=
```
