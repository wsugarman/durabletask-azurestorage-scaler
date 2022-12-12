param
(
    [Parameter(Mandatory=$True)]
    [string]
    $HelmChartPath,

    [Parameter(Mandatory=$False)]
    [Nullable[int]]
    $PullRequestNumber = $null
)

# Turn off trace and stop on any error
Set-PSDebug -Off
$ErrorActionPreference = "Stop"

$line = Get-Content $HelmChartPath | Select-String -Pattern '^appVersion: (?<appVersion>.+)$' | Select-Object -First 1
if (!$line || !$line.Matches.Success)
{
    throw [InvalidOperationException]::new("Cannot find appVersion in helm chart '$HelmChartPath'")
}

$tagVersion = $line.Matches.Groups[1].Value.Trim()
if (!($tagVersion -match '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<suffix>[a-zA-Z]+\.\d+))?$'))
{
    throw [InvalidOperationException]::new("Unexpected helm chart version '$tagVersion'")
}

$assemblyVersion = [string]::Format("{0}.0.0.0", $Matches.major)

# PR builds indicate that they're unofficial and pre-release via a change to their file and package versions.
if ($PullRequestNumber)
{
    $fileVersion    = [string]::Format("{0}.{1}.{2}.{3}"   , $Matches.major, $Matches.minor, $Matches.patch, $PullRequestNumber)
    $packageVersion = [string]::Format("{0}.{1}.{2}-pr.{3}", $Matches.major, $Matches.minor, $Matches.patch, $PullRequestNumber)
}
else
{
    $fileVersion    = [string]::Format("{0}.{1}.{2}.0", $Matches.major, $Matches.minor, $Matches.patch)
    $packageVersion = [string]::Format("{0}.{1}.{2}"  , $Matches.major, $Matches.minor, $Matches.patch)
    if (![string]::IsNullOrWhiteSpace($Matches.suffix))
    {
        $packageVersion += "-" + $Matches.suffix
    }
}

# Output environment variables to be used in the build step
"assembly=$assemblyVersion" >> $env:GITHUB_OUTPUT
"file=$fileVersion" >> $env:GITHUB_OUTPUT
"tag=$tagVersion" >> $env:GITHUB_OUTPUT
