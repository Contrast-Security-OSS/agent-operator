#!/bin/pwsh

$project = [System.IO.Path]::GetFullPath(".\src\Contrast.K8s.AgentOperator\Contrast.K8s.AgentOperator.csproj")
$output = [System.IO.Path]::GetFullPath(".\manifests\")

Write-Host "Project: $project"
Write-Host "Output: $output"

dotnet build $project

Write-Host "Build complete, generating crd."
Remove-Item -Recurse $output\crd\ -ErrorAction Ignore
dotnet run --project $project -- generator crd -o $output\crd\

Write-Host "Build complete, generating rbac."
Remove-Item -Recurse $output\rbac\ -ErrorAction Ignore
dotnet run --project $project -- generator rbac -o $output\rbac\

@(
    "$($output)crd\daemonsets_apps.yaml"
    "$($output)crd\deployments_apps.yaml"
    "$($output)crd\kustomization.yaml"
    "$($output)crd\secrets_.yaml"
    "$($output)crd\statefulsets_apps.yaml"
) | ForEach-Object {
    Write-Host "Cleaning up bad object $_"
    Remove-Item $_
}

Write-Host "Done."
