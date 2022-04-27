#!/bin/pwsh

$project = [System.IO.Path]::GetFullPath(".\src\Contrast.K8s.AgentOperator\Contrast.K8s.AgentOperator.csproj")
$output = [System.IO.Path]::GetFullPath(".\manifests\")

Write-Host "Project: $project"
Write-Host "Output: $output"

dotnet build $project

Write-Host "Generating crd."
Remove-Item -Recurse $output\crd\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator crd -o $output\crd\

# Write-Host "Generating docker."
# Remove-Item -Recurse $output\docker\ -ErrorAction Ignore
# dotnet run --no-build --project $project -- generator docker -o $output\docker\

Write-Host "Generating installer."
Remove-Item -Recurse $output\installer\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator installer -o $output\installer\

Write-Host "Generating operator."
Remove-Item -Recurse $output\operator\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator operator -o $output\operator\

Write-Host "Generating rbac."
Remove-Item -Recurse $output\rbac\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator rbac -o $output\rbac\

@(
    "$($output)crd\daemonsets_apps.yaml"
    "$($output)crd\deployments_apps.yaml"
    "$($output)crd\kustomization.yaml"
    "$($output)crd\secrets_.yaml"
    "$($output)crd\statefulsets_apps.yaml"
    "$($output)crd\pods_.yaml"
) | ForEach-Object {
    Write-Host "Cleaning up bad object $_"
    Remove-Item $_
}

Write-Host "Done."
