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
$chart = Get-Content -Path $ChartPath

# Find the fields in the YAML file
$appVersionResult = $chart | Select-String -Pattern '^appVersion\s*:' | Select-Object -First 1
if (-Not $appVersionResult.Matches.Success) {
    throw [InvalidOperationException]::new("Could not find top-level field 'appVersion' in chart '$ChartPath'.")
}

$versionResult = $chart | Select-String -Pattern '^version\s*:' | Select-Object -First 1
if (-Not $versionResult.Matches.Success) {
    throw [InvalidOperationException]::new("Could not find top-level field 'version' in chart '$ChartPath'.")
}

# Ensure the versions are valid semantic versions
$isValid = $appVersionResult.Line -match ('^appVersion\s*:\s*(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?<Suffix>-[a-zA-Z]+\.\d+)?)$')
if (-Not $isValid) {
    throw [FormatException]::new("'$($appVersionResult.Line)' denotes an invalid semantic version.")
}

$appVersionMatches = $Matches

$isValid = $versionResult.Line -match ('^version\s*:\s*(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?<Suffix>-[a-zA-Z]+\.\d+)?)$')
if (-Not $isValid) {
    throw [FormatException]::new("'$($versionResult.Line)' denotes an invalid semantic version.")
}

$versionMatches = $Matches

# Each version number for .NET is restricted to 16-bit numbers, so we'll only preserve the run id for the tag
# See here for details: https://learn.microsoft.com/en-us/windows/win32/menurc/versioninfo-resource
$assemblyFileVersion = "$($appVersionMatches.Major).$($appVersionMatches.Minor).$($appVersionMatches.Patch).0"

# Adjust the version for Pull Requests (by passing the workflow run id) to differentiate them from official builds
if ($WorkflowRunId) {
    $helmPrerelease = $True
    $helmVersion = "$($versionMatches.Major).$($versionMatches.Minor).$($versionMatches.Patch)-pr.$WorkflowRunId"
    $imagePrerelease = $True
    $imageTag = "$($appVersionMatches.Major).$($appVersionMatches.Minor).$($appVersionMatches.Patch)-pr.$WorkflowRunId"
}
else {
    $helmPrerelease = -Not [string]::IsNullOrEmpty($versionMatches.Suffix)
    $helmVersion = "$($versionMatches.Major).$($versionMatches.Minor).$($versionMatches.Patch)$($versionMatches.Suffix)"
    $imagePrerelease = -Not [string]::IsNullOrEmpty($appVersionMatches.Suffix)
    $imageTag = "$($appVersionMatches.Major).$($appVersionMatches.Minor).$($appVersionMatches.Patch)$($appVersionMatches.Suffix)"
}

# Create output variables
"assemblyFileVersion=$assemblyFileVersion" >> $env:GITHUB_OUTPUT
"helmPrerelease=$helmPrerelease" >> $env:GITHUB_OUTPUT
"helmVersion=$helmVersion" >> $env:GITHUB_OUTPUT
"imageTag=$imageTag" >> $env:GITHUB_OUTPUT
"imagePrerelease=$imagePrerelease" >> $env:GITHUB_OUTPUT
