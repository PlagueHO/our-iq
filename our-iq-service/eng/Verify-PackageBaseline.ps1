[CmdletBinding()]
param(
    [switch] $Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$serviceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $serviceRoot 'eng\PackageBaseline\OurIQ.PackageBaseline.csproj'
$nugetConfigPath = Join-Path $serviceRoot 'nuget.config'
$packageCachePath = Join-Path $serviceRoot '.nuget\packages'

if ($Clean -and (Test-Path -LiteralPath $packageCachePath)) {
    Remove-Item -LiteralPath $packageCachePath -Recurse -Force
}

$previousPackageCachePath = $env:NUGET_PACKAGES
$env:NUGET_PACKAGES = $packageCachePath

try {
    & dotnet restore $projectPath --configfile $nugetConfigPath --force-evaluate

    if ($LASTEXITCODE -ne 0) {
        throw "Package baseline restoration failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:NUGET_PACKAGES = $previousPackageCachePath
}
