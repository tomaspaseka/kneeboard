<#
.SYNOPSIS
    Bumps ApplicationDisplayVersion by minor version and ApplicationVersion by 1.
.EXAMPLE
    .\scripts\Bump-Version.ps1
#>

$ErrorActionPreference = 'Stop'
$csproj = Join-Path (Split-Path -Parent $PSScriptRoot) "Kneeboard\Kneeboard.csproj"

$xml = [xml](Get-Content $csproj)
$pg  = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1

$displayVersion = $pg.ApplicationDisplayVersion
$appVersion     = [int]$pg.ApplicationVersion

# Parse major.minor (support optional patch segment, ignore it)
$parts = $displayVersion -split '\.'
if ($parts.Count -lt 2) {
    Write-Error "ApplicationDisplayVersion '$displayVersion' must be in 'major.minor' format."
    exit 1
}
$major = [int]$parts[0]
$minor = [int]$parts[1]

$newDisplayVersion = "$major.$($minor + 1)"
$newAppVersion     = $appVersion + 1

$pg.ApplicationDisplayVersion = $newDisplayVersion
$pg.ApplicationVersion        = "$newAppVersion"

$xml.Save($csproj)

Write-Host "Version bumped:"
Write-Host "  ApplicationDisplayVersion  $displayVersion  ->  $newDisplayVersion"
Write-Host "  ApplicationVersion         $appVersion  ->  $newAppVersion"
