#!/bin/pwsh

Write-Host "Generating manifests."

./generate-manifests.ps1

Write-Host "Deploying manifests into cluster."

kubectl apply -k ./

Write-Host "Done."
