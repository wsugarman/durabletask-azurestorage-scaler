#!/usr/bin/env pwsh
param
(
    [Parameter(Mandatory=$False)]
    [ValidateNotNullOrEmpty()]
    [string]
    $ChartPath = (Join-Path $PSScriptRoot '..' '..' '..' '..' 'charts' 'durabletask-azurestorage-scaler' 'Chart.yaml'),

    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Destination,

    [Parameter(Mandatory=$False)]
    [ValidateNotNullOrEmpty()]
    [string]
    $DisplayName = 'Durable Task KEDA External Scaler',

    [Parameter(Mandatory=$False)]
    [ValidateNotNullOrEmpty()]
    [string]
    $License = 'MIT',

    [Parameter(Mandatory=$False)]
    [string]
    $LogoPath
)

# Turn off trace and stop on any error
Set-PSDebug -Off
$ErrorActionPreference = 'Stop'

# Import YAML module and parse the chart YAML into an object
Install-Module powershell-yaml -Scope CurrentUser
$chart = Get-Content -Path $chartPath -Raw | ConvertFrom-Yaml
$annotations = $chart['annotations']

# Source of the fields in the artifacthub-pkg.yml from the chart.yml
# See here for the chart.yaml schema: https://helm.sh/docs/topics/charts/#the-chart-file-structure
# See here for the artifacthub-pkg.yml schema: https://github.com/artifacthub/hub/blob/master/docs/metadata/artifacthub-pkg.yml
# See here for the available annotations: https://artifacthub.io/docs/topics/annotations/helm/
$pkg = [Hashtable]::new()

if ($chart['version']) {
    $pkg['version'] = $chart['version']
}

if ($chart['name']) {
    $pkg['name'] = $chart['name']
}

$pkg['displayName'] = $DisplayName
$pkg['createdAt'] =  (Get-Date -AsUTC).ToString("yyyy'-'MM'-'dd'T'HH:mm:ss'Z'")

if ($chart['description']) {
    $pkg['description'] = $chart['description']
}

if ($LogoPath) {
    $pkg['logoPath'] = $LogoPath
} elseif ($chart['description']) {
    $pkg['description'] = $chart['description']
}

if ($chart['icon']) {
    $pkg['logoURL'] = $chart['icon']
}

$pkg['license'] = $License

if ($chart['home']) {
    $pkg['homeURL'] = $chart['home']
}

if ($chart['appVersion']) {
    $pkg['appVersion'] = $chart['appVersion']
}

if ($annotations['artifacthub.io/images']) {
    $pkg['containersImages'] = $annotations['artifacthub.io/images']
}

if ($annotations.Contains('artifacthub.io/containsSecurityUpdates')) {
    $pkg['containsSecurityUpdates'] = $annotations['artifacthub.io/containsSecurityUpdates']
}

if ($annotations['artifacthub.io/operator']) {
    $pkg['operator'] = $annotations['artifacthub.io/operator']
}

if ($chart.Contains('deprecated')) {
    $pkg['deprecated'] = $chart['deprecated']
}

if ($annotations.Contains('artifacthub.io/prerelease')) {
    $pkg['prerelease'] = $annotations['artifacthub.io/prerelease']
}

if ($chart['keywords']) {
    $pkg['keywords'] = $chart['keywords']
}

if ($annotations['artifacthub.io/links']) {
    $pkg['links'] = $annotations['artifacthub.io/links']
}

if ($annotations['artifacthub.io/changes']) {
    $pkg['changes'] = $annotations['artifacthub.io/changes']
}

if ($chart['maintainers']) {
    $pkg['maintainers'] = $chart['maintainers']
}

if ($annotations['artifacthub.io/recommendations']) {
    $pkg['recommendations'] = $annotations['artifacthub.io/recommendations']
}

# Output the file as YAML
ConvertTo-Yaml -Data $pkg -OutFile (Join-Path $Destination 'artifacthub-pkg.yml') -Force
