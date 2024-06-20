#!/usr/bin/pwsh

param(
    [string]$ChartVersion = "0.0.2",
    [string]$AppVersion = "0.0.2"
)

function Invoke-Kustomize([string] $BasePath, [string] $OutputFilePath)
{
    kubectl kustomize ([System.IO.Path]::GetFullPath($BasePath)) | Out-File -Append -FilePath $OutputFilePath
}

$root = $PSScriptRoot

# Generate Manifests
Write-Host "Generating manifests..."
Invoke-Kustomize -BasePath "$root/build/crds" -OutputFilePath "$root/crds/generated.yaml"
Write-Output "{{ if ne .Values.operator.enabled false }}" | Out-File -FilePath "$root/templates/generated.yaml"
Invoke-Kustomize -BasePath "$root/build/templates" -OutputFilePath "$root/templates/generated.yaml"
Write-Output "{{ end }}" | Out-File -Append -FilePath "$root/templates/generated.yaml"

# Package
Write-Host "Linting chart."
helm lint $root --values "$root/values.testing.yaml"
if (-Not $?)
{
    throw "Linting failed."
}

Write-Host "Packaging chart."
helm package `
    $root `
    --app-version $AppVersion `
    --version $ChartVersion `
    --destination "$root/dist"
if (-Not $?)
{
    throw "Packing failed."
}
