#!/bin/pwsh
# Run under powershell core
#Requires -Version 6.0

$project = [System.IO.Path]::GetFullPath("..\src\Contrast.K8s.AgentOperator\Contrast.K8s.AgentOperator.csproj")
$output = [System.IO.Path]::GetFullPath(".\generated\")

Write-Host "Project: $project"
Write-Host "Output: $output"

dotnet kubeops generate operator contrast-agent-operator $project --out $output

@(
    "$($output)*.pem"
    "$($output)Dockerfile"
    "$($output)kustomization.yaml"
) | ForEach-Object {
    Write-Host "Cleaning up object $_"
    Remove-Item $_
}

Write-Host "Done. Compare with manifests in install folder and merge changes."
