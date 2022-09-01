#!/usr/bin/pwsh

param(
    [string]$ChartVersion = "0.0.2",
    [string]$AppVersion = "0.0.2"
)

function Invoke-Kustomize([string] $BasePath, [string] $OutputFilePath)
{
    kubectl kustomize ([System.IO.Path]::GetFullPath($BasePath)) | Out-File -FilePath $OutputFilePath
}

$root = $PSScriptRoot

# Generate Manifests
Write-Host "Generating manifests..."
Invoke-Kustomize -BasePath "$root/build/crds" -OutputFilePath "$root/crds/generated.yaml"
Invoke-Kustomize -BasePath "$root/build/templates" -OutputFilePath "$root/templates/generated.yaml"

# Package
Write-Host "Linting chart."
helm lint $root

Write-Host "Packaging chart."
helm package `
    $root `
    --app-version $AppVersion `
    --version $ChartVersion `
    --destination "$root/dist"
