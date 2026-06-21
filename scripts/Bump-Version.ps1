<#
.SYNOPSIS
    Bumps the REVISION component of the MAJOR.MINOR.REVISION version scheme.
    Called automatically by Publish-Release.ps1 before each release build.
    Invoke manually only when you need to test a version bump without publishing.
.EXAMPLE
    .\scripts\Bump-Version.ps1
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

$newRevision       = $revision + 1
$newDisplayVersion = "$major.$minor.$newRevision"
$newAppVersion     = "$newRevision"

$pg.ApplicationDisplayVersion = $newDisplayVersion
$pg.ApplicationVersion        = $newAppVersion

$xml.Save($csproj)

Write-Host "Version bumped:"
Write-Host "  ApplicationDisplayVersion  $displayVersion  ->  $newDisplayVersion"
Write-Host "  ApplicationVersion         $revision  ->  $newRevision"
