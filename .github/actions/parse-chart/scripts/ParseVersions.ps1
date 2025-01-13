#!/usr/bin/env pwsh
param
(
    [Parameter(Mandatory=$False)]
    [ValidateNotNullOrEmpty()]
    [string]
    $ChartPath = (Join-Path $PSScriptRoot '..' '..' '..' '..' 'charts' 'durabletask-azurestorage-scaler' 'Chart.yaml'),

    [Parameter(Mandatory=$False)]
    [string]
    $WorkflowRunId = $null
)

# Turn off trace and stop on any error
Set-PSDebug -Off
$ErrorActionPreference = "Stop"

# Read the Chart.yaml
# Note: Explicitly set TLS 1.2 based on https://rnelson0.com/2018/05/17/powershell-in-a-post-tls1-1-world/
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Install-Module powershell-yaml -Force -Repository PSGallery -Scope CurrentUser
$chart = Get-Content -Path $chartPath -Raw | ConvertFrom-Yaml -Ordered

# Find the fields in the YAML file
$appVersion = $chart['appVersion']
if (-Not $appVersion) {
    throw [InvalidOperationException]::new("Could not find field 'appVersion' in chart '$ChartPath'.")
}

$version = $chart['version']
if (-Not $version) {
    throw [InvalidOperationException]::new("Could not find field 'version' in chart '$ChartPath'.")
}

# Ensure the versions are valid semantic versions
$isValid = $appVersion -match '^(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?<Suffix>-[a-zA-Z]+\.\d+)?$'
if (-Not $isValid) {
    throw [FormatException]::new("'$appVersion' denotes an invalid semantic version.")
}

$appVersionMatches = $Matches

$isValid = $version -match '^(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?<Suffix>-[a-zA-Z]+\.\d+)?$'
if (-Not $isValid) {
    throw [FormatException]::new("'$version' denotes an invalid semantic version.")
}

$versionMatches = $Matches

# Also ensure that the container image tag is up-to-date
$annotations = $chart['annotations']
if ($annotations -And $annotations['artifacthub.io/images']) {
    $images = ConvertFrom-Yaml -Ordered $annotations['artifacthub.io/images']
    $image = $images | Where-Object {$_.name -eq 'durabletask-azurestorage-scaler'} | Select-Object @{Name='image';Expression={$_.image}} | Select-Object -ExpandProperty image -First 1

    if ($image -And -Not $image.EndsWith(':' + $appVersion)) {
        throw [InvalidOperationException]::new("Tag for image 'durabletask-azurestorage-scaler' does not match appVersion '$appVersion'.")
    }
}

# Each version number for .NET is restricted to 16-bit numbers, so we'll only preserve the run id for the tag
# See here for details: https://learn.microsoft.com/en-us/windows/win32/menurc/versioninfo-resource
$assemblyFileVersion = "$($appVersionMatches.Major).$($appVersionMatches.Minor).$($appVersionMatches.Patch).0"
$assemblyVersion = "$($appVersionMatches.Major).0.0.0"

# Adjust the version for Pull Requests (by passing the workflow run id) to differentiate them from official builds
if ($WorkflowRunId) {
    $chartPrerelease = $True
    $chartVersion = "$($versionMatches.Major).$($versionMatches.Minor).$($versionMatches.Patch)-pr.$WorkflowRunId"
    $imagePrerelease = $True
    $imageTag = "$($appVersionMatches.Major).$($appVersionMatches.Minor).$($appVersionMatches.Patch)-pr.$WorkflowRunId"
}
else {
    $chartPrerelease = -Not [string]::IsNullOrEmpty($versionMatches.Suffix)
    $chartVersion = "$($versionMatches.Major).$($versionMatches.Minor).$($versionMatches.Patch)$($versionMatches.Suffix)"
    $imagePrerelease = -Not [string]::IsNullOrEmpty($appVersionMatches.Suffix)
    $imageTag = "$($appVersionMatches.Major).$($appVersionMatches.Minor).$($appVersionMatches.Patch)$($appVersionMatches.Suffix)"
}

# Create output variables
"assemblyFileVersion=$assemblyFileVersion" >> $env:GITHUB_OUTPUT
"assemblyVersion=$assemblyVersion" >> $env:GITHUB_OUTPUT
"chartPrerelease=$chartPrerelease" >> $env:GITHUB_OUTPUT
"chartVersion=$chartVersion" >> $env:GITHUB_OUTPUT
"imageTag=$imageTag" >> $env:GITHUB_OUTPUT
"imagePrerelease=$imagePrerelease" >> $env:GITHUB_OUTPUT
