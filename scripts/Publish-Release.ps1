<#
.SYNOPSIS
    Bumps the release revision, builds the MSIX, and packages it with Install.ps1
    into dist\Kneeboard-v<major.minor.revision>.zip.
.EXAMPLE
    .\scripts\Publish-Release.ps1
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Auto-bump the revision before building so every published package is strictly
# newer than the previous one and Add-AppxPackage upgrades rather than rejecting.
Write-Host "Bumping release revision..."
& "$root\scripts\Bump-Version.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Read the bumped version from csproj
$csproj     = Join-Path $root "Kneeboard\Kneeboard.csproj"
$xml        = [xml](Get-Content $csproj)
$pg         = $xml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1
$version    = $pg.ApplicationDisplayVersion   # e.g. "1.1.1"
$appVersion = [int]$pg.ApplicationVersion     # e.g. 1  (the revision component)

Write-Host "=== Kneeboard release build v$version ==="

# Clean stale build outputs so the MAUI resizetizer regenerates Package.appxmanifest
# with the new version. Without this, a version-only csproj change is invisible to
# the resizetizer's input-tracking and the MSIX Identity Version stays frozen at the
# previously cached value.
Write-Host "Cleaning previous build output..."
foreach ($p in @("$root\Kneeboard\AppPackages", "$root\Kneeboard\obj", "$root\Kneeboard\bin")) {
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}

# Two-step publish: restore without RID first (workaround for MAUI Mono-restore bug),
# then publish self-contained without re-restoring.
Write-Host "Restoring packages..."
dotnet restore "$root\Kneeboard" -p:TargetFramework=net10.0-windows10.0.19041.0
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running dotnet publish..."
dotnet publish "$root\Kneeboard" `
    -f net10.0-windows10.0.19041.0 `
    -c Release `
    -p:PublishProfile=WinRelease `
    -p:SelfContained=true `
    -p:WindowsAppSDKSelfContained=false `
    --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# MAUI Windows MSIX output lands in AppPackages/, not in bin/Release/
$searchRoot = Join-Path $root "Kneeboard\AppPackages"
$msix = Get-ChildItem $searchRoot -Recurse -Filter "*.msix" |
        Where-Object { $_.Name -notlike "Microsoft.*" } |
        Select-Object -First 1

if (-not $msix) {
    Write-Error "No .msix found under $searchRoot — publish may have failed."
    exit 1
}
Write-Host "Found MSIX: $($msix.FullName)"

# Guard: verify the MSIX was built with the revision we just bumped.
# MAUI names the file  Kneeboard_<Identity.Version>_<arch>.msix.
# The Identity Version is 4-part (major.minor.build.appVersion); the 4th
# component is always ApplicationVersion. Check only that so the guard doesn't
# depend on how MAUI maps the display version's 3rd component.
if ($msix.Name -match '_(\d+\.\d+\.\d+\.(\d+))_') {
    $msixIdentityVersion = $Matches[1]
    $msixAppVersion      = [int]$Matches[2]
    if ($msixAppVersion -ne $appVersion) {
        Write-Error @"
MSIX version mismatch.
  Expected ApplicationVersion : $appVersion  (display $version)
  MSIX Identity Version       : $msixIdentityVersion
The manifest was not regenerated — stale resizetizer cache survived the clean.
Aborting to avoid shipping a package that cannot upgrade existing installs.
"@
        exit 1
    }
} else {
    Write-Error "MSIX filename '$($msix.Name)' does not contain a 4-part version — unexpected format."
    exit 1
}

# Stage release files in a temp folder
$stage = Join-Path $env:TEMP "KnbRelease-$version"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $stage | Out-Null

Copy-Item $msix.FullName              "$stage\Kneeboard.msix"
Copy-Item "$root\Kneeboard.cer"       "$stage\Kneeboard.cer"
Copy-Item "$root\scripts\Install.ps1" "$stage\Install.ps1"
Copy-Item "$root\scripts\Install.cmd" "$stage\Install.cmd"

# Zip
$distDir = Join-Path $root "dist"
New-Item -ItemType Directory -Force $distDir | Out-Null
$zipPath = Join-Path $distDir "Kneeboard-v$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "$stage\*" -DestinationPath $zipPath

Remove-Item $stage -Recurse -Force

Write-Host ""
Write-Host "Release artifact: $zipPath"
Write-Host "Contents:"
(Get-ChildItem (Join-Path $root "dist")).Name | ForEach-Object { Write-Host "  $_" }
