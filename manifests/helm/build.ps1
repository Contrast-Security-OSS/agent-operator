#!/usr/bin/pwsh

param(
    [string]$ChartVersion = "0.0.2",
    [string]$AppVersion = "0.0.2",
    [switch]$WriteSchemaUrl
)

function Invoke-Kustomize([string] $BasePath, [string] $OutputFilePath)
{
    kubectl kustomize ([System.IO.Path]::GetFullPath($BasePath)) | Out-File -Append -FilePath $OutputFilePath
}

$root = $PSScriptRoot

# Generate CRDs
Write-Host "Generating CRDs..."
Invoke-Kustomize -BasePath "$root/build/crds" -OutputFilePath "$root/crds/generated.yaml"

# Schema
if($WriteSchemaUrl)
{
    Write-Host "Updating values.yaml schema url"
    $content = Get-Content -Path "$root/values.yaml"
    $schemaText = [string]::Format('# yaml-language-server: $schema=https://github.com/Contrast-Security-OSS/agent-operator/releases/download/v{0}/values.schema.json',$AppVersion)
    $content = $content.Replace('# yaml-language-server: $schema=values.schema.json', $schemaText)
    Set-Content -Path "$root/values.yaml" -Value $content
}

Write-Host "Copying schema."
Copy-Item "$root/values.schema.json" -Destination "$root/dist/contrast-agent-operator-$ChartVersion.schema.json"

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


