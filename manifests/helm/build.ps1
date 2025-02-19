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

# Generate CRDs
Write-Host "Generating CRDs..."
Invoke-Kustomize -BasePath "$root/build/crds" -OutputFilePath "$root/crds/generated.yaml"

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
