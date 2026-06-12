# Distribution Design — Windows MSIX Sideload

**Date:** 2026-06-12  
**Scope:** Windows only. Android excluded for now.

## Goal

Distribute the Kneeboard app to other Windows machines with all dependencies bundled. Recipients run a single PowerShell script (one UAC prompt) to install or update.

## Package Identity

- **ApplicationId:** `com.tpaseka.kneeboard`
- **Publisher:** `CN=tpaseka`
- **ApplicationDisplayVersion:** human-readable semver (e.g. `1.0`, `1.1`)
- **ApplicationVersion:** monotonically-increasing integer (e.g. `1`, `2`)

## Build & Packaging

Publish target: `net10.0-windows10.0.19041.0`, `Release` configuration.

Key MSBuild properties:
- `SelfContained=true` — bundles .NET runtime; no .NET prerequisite on target machine
- `WindowsAppSDKSelfContained=true` — bundles Windows App SDK runtime; no WinAppSDK prerequisite on target machine
- `GenerateAppxPackageOnBuild=true` — produces `.msix` alongside publish output

A Windows publish profile (`Kneeboard/Properties/PublishProfiles/WinRelease.pubxml`) captures these settings so the build is a single command:

```powershell
dotnet publish Kneeboard -f net10.0-windows10.0.19041.0 -c Release
```

## Certificate & Signing

MSIX packages must be signed; Windows will not install unsigned packages.

- A self-signed certificate is generated once via `scripts/New-DevCert.ps1` and stored locally as `Kneeboard.pfx` (never committed — added to `.gitignore`).
- The `.pfx` path and password are passed to `dotnet publish` via MSBuild properties at build time (environment variables or a local `.props` file, also gitignored).
- The public `.cer` is not shipped separately — the install script extracts it from the MSIX at install time.

## Install Script

Release artifact (`Kneeboard-v<version>.zip`):

```
Kneeboard-v1.0.zip
├── Kneeboard.msix
└── Install.ps1
```

`Install.ps1` steps (runs elevated — one UAC prompt):
1. Extracts the signing certificate from the MSIX package.
2. Imports it into `Cert:\LocalMachine\TrustedPeople` (idempotent — skips if already present).
3. Calls `Add-AppxPackage` to install or upgrade the app.
4. Prints a clear success or failure message.

Usage: right-click `Install.ps1` → **Run with PowerShell**, approve UAC. Works for first install and all future updates.

## Release Process

1. Bump `ApplicationDisplayVersion` and `ApplicationVersion` in `Kneeboard.csproj`.
2. Run `dotnet publish Kneeboard -f net10.0-windows10.0.19041.0 -c Release`.
3. Zip the `.msix` + `Install.ps1` into `Kneeboard-v<version>.zip` and distribute.

A helper script `scripts/Publish-Release.ps1` automates steps 2–3.

## Out of Scope

- Auto-update / delta patching
- Microsoft Store submission
- Android distribution
- CI/CD pipeline
