<#
.SYNOPSIS
    Bumps the MINOR component of the MAJOR.MINOR.REVISION version scheme.
    REVISION is reset to 0 (e.g. 1.1.5 -> 1.2.0).
    Invoke manually when starting a new minor release cycle.
.EXAMPLE
    .\scripts\Bump-Minor.ps1
#>

$ErrorActionPreference = 'Stop'
$csproj = Join-Path (Split-Path -Parent $PSScriptRoot) "Kneeboard\Kneeboard.csproj"

$xml = [xml](Get-Content $csproj)
$pg  = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1

$displayVersion = $pg.ApplicationDisplayVersion

# Parse major.minor.revision  (revision defaults to 0 if missing)
$parts = $displayVersion -split '\.'
if ($parts.Count -lt 2) {
    Write-Error "ApplicationDisplayVersion '$displayVersion' must be at least 'major.minor' format."
    exit 1
}
$major    = [int]$parts[0]
$minor    = [int]$parts[1]
$revision = if ($parts.Count -ge 3) { [int]$parts[2] } else { 0 }

$newMinor          = $minor + 1
$newDisplayVersion = "$major.$newMinor.0"

$pg.ApplicationDisplayVersion = $newDisplayVersion
$pg.ApplicationVersion        = "0"

$xml.Save($csproj)

Write-Host "Version bumped:"
Write-Host "  ApplicationDisplayVersion  $displayVersion  ->  $newDisplayVersion"
Write-Host "  ApplicationVersion         $revision  ->  0"
