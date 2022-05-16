#!/bin/pwsh

$project = [System.IO.Path]::GetFullPath("..\src\Contrast.K8s.AgentOperator\Contrast.K8s.AgentOperator.csproj")
$output = [System.IO.Path]::GetFullPath(".\generated\")

Write-Host "Project: $project"
Write-Host "Output: $output"

dotnet build $project

Write-Host "Generating crd."
Remove-Item -Recurse $output\crd\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator crd -o $output\crd\

# Write-Host "Generating docker."
# Remove-Item -Recurse $output\docker\ -ErrorAction Ignore
# dotnet run --no-build --project $project -- generator docker -o $output\docker\

# Write-Host "Generating installer."
# Remove-Item -Recurse $output\installer\ -ErrorAction Ignore
# dotnet run --no-build --project $project -- generator installer -o $output\installer\

Write-Host "Generating operator."
Remove-Item -Recurse $output\operator\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator operator -o $output\operator\

Write-Host "Generating rbac."
Remove-Item -Recurse $output\rbac\ -ErrorAction Ignore
dotnet run --no-build --project $project -- generator rbac -o $output\rbac\

@(
    "$($output)operator\ca-key.pem"
    "$($output)operator\ca.csr"
    "$($output)operator\ca.pem"
    "$($output)operator\kustomization.yaml"
) | ForEach-Object {
    Write-Host "Cleaning up bad object $_"
    Remove-Item $_
}

Write-Host "Done. Make sure to copy these generated manifests into the install folder."
