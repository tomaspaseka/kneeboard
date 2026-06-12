<#
.SYNOPSIS
    Builds the MSIX and packages it with Install.ps1 into dist\Kneeboard-v<version>.zip.
.EXAMPLE
    .\scripts\Publish-Release.ps1
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Read version from csproj
$csproj  = Join-Path $root "Kneeboard\Kneeboard.csproj"
$xml     = [xml](Get-Content $csproj)
$version = ($xml.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1).ApplicationDisplayVersion

Write-Host "=== Kneeboard release build v$version ==="

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
    -p:WindowsAppSDKSelfContained=true `
    --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# MAUI Windows MSIX output lands in AppPackages/, not in bin/Release/
$searchRoot = Join-Path $root "Kneeboard\AppPackages"
$msix = Get-ChildItem $searchRoot -Recurse -Filter "*.msix" | Select-Object -First 1

if (-not $msix) {
    Write-Error "No .msix found under $searchRoot — publish may have failed."
    exit 1
}
Write-Host "Found MSIX: $($msix.FullName)"

# Stage release files in a temp folder
$stage = Join-Path $env:TEMP "KnbRelease-$version"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $stage | Out-Null

Copy-Item $msix.FullName              "$stage\Kneeboard.msix"
Copy-Item "$root\Kneeboard.cer"       "$stage\Kneeboard.cer"
Copy-Item "$root\scripts\Install.ps1" "$stage\Install.ps1"

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
