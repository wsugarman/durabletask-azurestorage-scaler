#!/usr/bin/env pwsh
param
(
    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $ChartVersion,

    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Destination,

    [Parameter(Mandatory=$False)]
    [ValidateNotNullOrEmpty()]
    [string]
    $DisplayName = 'Durable Task KEDA External Scaler',

    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $IndexPath,

    [Parameter(Mandatory=$False)]
    [string]
    $LogoPath
)

# Turn off trace and stop on any error
Set-PSDebug -Off
$ErrorActionPreference = 'Stop'

# Import YAML module and parse the chart YAML into an object
Install-Module powershell-yaml -Force -Repository PSGallery -Scope CurrentUser
$index = Get-Content -Path $IndexPath -Raw | ConvertFrom-Yaml -Ordered

$chart = $index['entries']['durabletask-azurestorage-scaler'] | Where-Object {$_['version'] -eq $ChartVersion} | Select-Object -First 1
if (-Not $chart) {
    throw [InvalidOperationException]::new("Cannot find entry for chart version '$ChartVersion' in index.yaml.")
}

$annotations = $chart['annotations']

# Source of the fields in the artifacthub-pkg.yml from the chart.yml
# See here for the chart.yaml schema: https://helm.sh/docs/topics/charts/#the-chart-file-structure
# See here for the artifacthub-pkg.yml schema: https://github.com/artifacthub/hub/blob/master/docs/metadata/artifacthub-pkg.yml
# See here for the available annotations: https://artifacthub.io/docs/topics/annotations/helm/
$pkg = [System.Collections.Specialized.OrderedDictionary]::new([StringComparer].OrdinalIgnoreCase)

if ($chart['version']) {
    $pkg['version'] = $chart['version']
}

if ($chart['name']) {
    $pkg['name'] = $chart['name']
}

if ($annotations['artifacthub.io/category']) {
    $pkg['category'] = $annotations['artifacthub.io/category']
}

$pkg['displayName'] = $DisplayName
$pkg['createdAt'] =  (Get-Date -AsUTC).ToString("yyyy'-'MM'-'dd'T'HH:mm:ss'Z'")

if ($chart['description']) {
    $pkg['description'] = $chart['description']
}

if ($LogoPath) {
    $pkg['logoPath'] = $LogoPath
}
elseif ($chart['icon']) {
    $pkg['logoURL'] = $chart['icon']
}

if ($chart['digest']) {
    $pkg['digest'] = $chart['digest']
}

if ($annotations['artifacthub.io/license']) {
    $pkg['license'] = $annotations['artifacthub.io/license']
}

if ($chart['home']) {
    $pkg['homeURL'] = $chart['home']
}

if ($chart['appVersion']) {
    $pkg['appVersion'] = $chart['appVersion']
}

if ($annotations['artifacthub.io/images']) {
    $pkg['containersImages'] = $annotations['artifacthub.io/images'] | ConvertFrom-Yaml -Ordered
}

if ($annotations.Contains('artifacthub.io/containsSecurityUpdates')) {
    $pkg['containsSecurityUpdates'] = [bool]::Parse($annotations['artifacthub.io/containsSecurityUpdates'])
}

if ($annotations.Contains('artifacthub.io/operator')) {
    $pkg['operator'] = [bool]::Parse($annotations['artifacthub.io/operator'])
}

if ($chart.Contains('deprecated')) {
    $pkg['deprecated'] = $chart['deprecated']
}

if ($annotations.Contains('artifacthub.io/prerelease')) {
    $pkg['prerelease'] = [bool]::Parse($annotations['artifacthub.io/prerelease'])
}

if ($chart['keywords']) {
    $pkg['keywords'] = $chart['keywords']
}

if ($annotations['artifacthub.io/links']) {
    $pkg['links'] = $annotations['artifacthub.io/links'] | ConvertFrom-Yaml -Ordered
}

$pkg['install'] = @"
Add repository
``````bash
helm repo add wsugarman https://wsugarman.github.io/charts
``````

Install chart
``````bash
helm install dtfx-scaler wsugarman/durabletask-azurestorage-scaler --version $ChartVersion
``````

***dtfx-scaler** corresponds to the release name, feel free to change it to suit your needs. You can also add additional flags to the **helm install** command if you need to.*

*Before you can use the scaler, you must ensure that KEDA is also installed in the Kubernetes cluster.*
"@

if ($annotations['artifacthub.io/changes']) {
    $pkg['changes'] = $annotations['artifacthub.io/changes'] | ConvertFrom-Yaml -Ordered
}

if ($chart['maintainers']) {
    $pkg['maintainers'] = $chart['maintainers']
}

if ($annotations['artifacthub.io/recommendations']) {
    $pkg['recommendations'] = $annotations['artifacthub.io/recommendations'] | ConvertFrom-Yaml -Ordered
}

# Output the file as YAML
ConvertTo-Yaml -Data $pkg -OutFile (Join-Path $Destination 'artifacthub-pkg.yml') -Force
