param
(
    [Parameter(Mandatory=$True)]
    [string]
    $VersionFilePath,

    [Parameter(Mandatory=$False)]
    [Nullable[int]]
    $PullRequestNumber = $null
)

# Turn off trace and stop on any error
Set-PSDebug -Off
$ErrorActionPreference = "Stop"

$versionParts = Get-Content $VersionFilePath | Out-String | ConvertFrom-Json
$assemblyVersion = [string]::Format("{0}.0.0.0", $versionParts.major)

# PR builds indicate that they're unofficial and pre-release via a change to their file and package versions.
if ($PullRequestNumber)
{
    $fileVersion    = [string]::Format("{0}.{1}.{2}.{3}"   , $versionParts.major, $versionParts.minor, $versionParts.patch, $PullRequestNumber)
    $packageVersion = [string]::Format("{0}.{1}.{2}-pr.{3}", $versionParts.major, $versionParts.minor, $versionParts.patch, $PullRequestNumber)
}
else
{
    $fileVersion    = [string]::Format("{0}.{1}.{2}.0", $versionParts.major, $versionParts.minor, $versionParts.patch)
    $packageVersion = [string]::Format("{0}.{1}.{2}"  , $versionParts.major, $versionParts.minor, $versionParts.patch)
    if (![string]::IsNullOrWhiteSpace($versionParts.suffix))
    {
        $packageVersion += "-" + $versionParts.suffix
    }
}

# Output variables to be used in the build
Write-Host "::set-output name=assembly::$assemblyVersion"
Write-Host "::set-output name=file::$fileVersion"
Write-Host "::set-output name=package::$packageVersion"
