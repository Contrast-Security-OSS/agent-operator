# Manifests

This directory contains manifests to install the operator into a cluster and examples on how to configure the operator.

## Development

Install manifests for local development:

```
kubectl apply -k .\install\dev
kubectl apply -k .\examples\dev
```

A pull secret is required to pull our agent images:
```
kubectl create secret docker-registry contrastdotnet-pull-secret --docker-server=contrastdotnet.azurecr.io --docker-username= --docker-password=
```
