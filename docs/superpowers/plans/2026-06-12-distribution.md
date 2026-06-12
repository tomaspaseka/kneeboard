# Distribution (Windows MSIX Sideload) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Package Kneeboard as a self-contained Windows MSIX and provide scripts so recipients can install it with a single PowerShell run (one UAC prompt).

**Architecture:** A WinRelease publish profile drives `dotnet publish` to produce a self-contained MSIX (bundles .NET + Windows App SDK runtimes). A self-signed certificate signs the package; `scripts/Install.ps1` imports the cert and installs the MSIX on the target machine. `scripts/Publish-Release.ps1` automates the full release build and zips the artifact.

**Tech Stack:** .NET MAUI 10, MSBuild publish profiles, PowerShell 5.1+, Windows MSIX/AppX APIs

> **Note:** The design spec described extracting the `.cer` from the MSIX at install time. This plan ships `Kneeboard.cer` separately in the release zip instead — the MSIX PKCS#7 signature format makes runtime extraction unreliable in PowerShell without additional assemblies. The user experience is identical.

---

## File Map

| Action | Path | Purpose |
|--------|------|---------|
| Modify | `Kneeboard/Kneeboard.csproj` | Fix ApplicationId; import Signing.props |
| Create | `Kneeboard/Properties/PublishProfiles/WinRelease.pubxml` | Windows self-contained publish settings |
| Create | `Kneeboard/Signing.props.example` | Template for local cert config (committed) |
| Create (gitignored) | `Kneeboard/Signing.props` | Developer's actual cert path + password |
| Modify | `.gitignore` | Ignore *.pfx, Signing.props, dist/ |
| Create | `scripts/New-DevCert.ps1` | One-time cert generation |
| Create | `scripts/Install.ps1` | Recipient install script |
| Create | `scripts/Publish-Release.ps1` | Release build + zip helper |
| Create (gitignored) | `Kneeboard.pfx` | Private signing key (never committed) |
| Create (committed) | `Kneeboard.cer` | Public cert shipped to recipients |

---

## Task 1: Fix application identity

**Files:**
- Modify: `Kneeboard/Kneeboard.csproj`

- [ ] **Step 1: Update ApplicationId**

  In `Kneeboard/Kneeboard.csproj`, change line 16:

  ```xml
  <ApplicationId>com.tpaseka.kneeboard</ApplicationId>
  ```

- [ ] **Step 2: Verify the build still passes**

  ```powershell
  dotnet build Kneeboard -f net10.0-windows10.0.19041.0 -c Release
  ```

  Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

  ```powershell
  git add Kneeboard/Kneeboard.csproj
  git commit -m "chore: set ApplicationId to com.tpaseka.kneeboard"
  ```

---

## Task 2: Update .gitignore

**Files:**
- Modify: `.gitignore`

- [ ] **Step 1: Append entries**

  Add to the bottom of `.gitignore`:

  ```gitignore
  # Code signing — private key and local config stay off source control
  *.pfx
  Kneeboard/Signing.props

  # Release artifacts
  dist/
  ```

- [ ] **Step 2: Commit**

  ```powershell
  git add .gitignore
  git commit -m "chore: ignore pfx, Signing.props, and dist/"
  ```

---

## Task 3: Create WinRelease publish profile

**Files:**
- Create: `Kneeboard/Properties/PublishProfiles/WinRelease.pubxml`

- [ ] **Step 1: Create the publish profile**

  Create `Kneeboard/Properties/PublishProfiles/WinRelease.pubxml`:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <Project>
    <PropertyGroup>
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
      <RuntimeIdentifier>win-x64</RuntimeIdentifier>
      <SelfContained>true</SelfContained>
      <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
      <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
    </PropertyGroup>
  </Project>
  ```

- [ ] **Step 2: Verify dry-run (signing will fail — expected until cert exists)**

  ```powershell
  dotnet publish Kneeboard -f net10.0-windows10.0.19041.0 -c Release -p:PublishProfile=WinRelease
  ```

  Expected: fails at signing step with a message about a missing certificate — this is correct; the cert doesn't exist yet.

- [ ] **Step 3: Commit**

  ```powershell
  git add Kneeboard/Properties/PublishProfiles/WinRelease.pubxml
  git commit -m "chore: add WinRelease publish profile (self-contained MSIX)"
  ```

---

## Task 4: Wire certificate signing into csproj

**Files:**
- Create: `Kneeboard/Signing.props.example`
- Modify: `Kneeboard/Kneeboard.csproj`

- [ ] **Step 1: Create the signing props template**

  Create `Kneeboard/Signing.props.example`:

  ```xml
  <!-- Copy this file to Signing.props (same directory) and fill in your local paths.
       Signing.props is gitignored — never commit it. -->
  <Project>
    <PropertyGroup>
      <!-- Absolute or repo-relative path to your .pfx file. -->
      <PackageCertificateKeyFile>$(MSBuildProjectDirectory)\..\Kneeboard.pfx</PackageCertificateKeyFile>
      <!-- Leave empty if you generated the cert with no password. -->
      <PackageCertificatePassword></PackageCertificatePassword>
    </PropertyGroup>
  </Project>
  ```

- [ ] **Step 2: Import Signing.props in the csproj**

  In `Kneeboard/Kneeboard.csproj`, add this line immediately after the opening `<Project ...>` tag (before the first `<PropertyGroup>`):

  ```xml
  <Import Project="Signing.props" Condition="Exists('$(MSBuildProjectDirectory)\Signing.props')" />
  ```

- [ ] **Step 3: Verify build still succeeds without Signing.props present**

  ```powershell
  dotnet build Kneeboard -f net10.0-windows10.0.19041.0 -c Release
  ```

  Expected: `Build succeeded.` (Signing.props absent → import skipped silently.)

- [ ] **Step 4: Commit**

  ```powershell
  git add Kneeboard/Signing.props.example Kneeboard/Kneeboard.csproj
  git commit -m "chore: add Signing.props.example and conditional import in csproj"
  ```

---

## Task 5: Create scripts/New-DevCert.ps1

**Files:**
- Create: `scripts/New-DevCert.ps1`

This script is run **once** by the developer to generate the signing cert pair. It produces `Kneeboard.pfx` (gitignored) and `Kneeboard.cer` (committed, shipped to recipients).

- [ ] **Step 1: Create the script**

  Create `scripts/New-DevCert.ps1`:

  ```powershell
  <#
  .SYNOPSIS
      Generates a self-signed code-signing certificate for Kneeboard MSIX sideloading.
      Run once from the repo root. Produces Kneeboard.pfx (gitignored) and Kneeboard.cer (commit this).
  .EXAMPLE
      .\scripts\New-DevCert.ps1
      .\scripts\New-DevCert.ps1 -Password "s3cret"
  #>
  param(
      [string]$Subject  = "CN=tpaseka",
      [string]$Password = ""
  )

  $ErrorActionPreference = 'Stop'
  $root   = Split-Path -Parent $PSScriptRoot
  $pfxOut = Join-Path $root "Kneeboard.pfx"
  $cerOut = Join-Path $root "Kneeboard.cer"

  Write-Host "Generating certificate: $Subject"

  $cert = New-SelfSignedCertificate `
      -Type CodeSigningCert `
      -Subject $Subject `
      -KeyUsage DigitalSignature `
      -FriendlyName "Kneeboard Dev Signing" `
      -CertStoreLocation "Cert:\CurrentUser\My" `
      -NotAfter (Get-Date).AddYears(10)

  # Export PFX (private key)
  $secPwd = ConvertTo-SecureString -String $Password -Force -AsPlainText
  Export-PfxCertificate -Cert $cert -FilePath $pfxOut -Password $secPwd | Out-Null

  # Export CER (public key — safe to commit and ship to recipients)
  Export-Certificate -Cert $cert -FilePath $cerOut -Type CERT | Out-Null

  Write-Host ""
  Write-Host "Created: $pfxOut  (gitignored — keep this private)"
  Write-Host "Created: $cerOut  (commit this; it goes in every release zip)"
  Write-Host ""
  Write-Host "Next: copy Kneeboard/Signing.props.example to Kneeboard/Signing.props"
  Write-Host "      and set PackageCertificateKeyFile = $pfxOut"
  if ($Password) {
      Write-Host "      and set PackageCertificatePassword = $Password"
  }
  ```

- [ ] **Step 2: Run it**

  ```powershell
  .\scripts\New-DevCert.ps1
  ```

  Expected output:
  ```
  Generating certificate: CN=tpaseka
  Created: C:\...\Kneeboard.pfx  (gitignored — keep this private)
  Created: C:\...\Kneeboard.cer  (commit this; it goes in every release zip)
  ```

  Verify both files appear in the repo root. Verify `Kneeboard.pfx` is listed as untracked but NOT staged (gitignore is working). Verify `Kneeboard.cer` is untracked and ready to add.

- [ ] **Step 3: Create Signing.props from the template**

  ```powershell
  Copy-Item Kneeboard\Signing.props.example Kneeboard\Signing.props
  ```

  Open `Kneeboard/Signing.props` and verify `PackageCertificateKeyFile` already points to the right path (`$(MSBuildProjectDirectory)\..\Kneeboard.pfx`). No changes needed if you used the defaults.

- [ ] **Step 4: Commit cert script and public cert**

  ```powershell
  git add scripts/New-DevCert.ps1 Kneeboard.cer
  git commit -m "chore: add cert generation script and initial Kneeboard.cer"
  ```

---

## Task 6: Create scripts/Install.ps1

**Files:**
- Create: `scripts/Install.ps1`

This is the script shipped inside every release zip. Recipients run it as Administrator.

- [ ] **Step 1: Create the script**

  Create `scripts/Install.ps1`:

  ```powershell
  <#
  .SYNOPSIS
      Installs or updates Kneeboard on this machine.
      Right-click this file and choose "Run with PowerShell". Approve the UAC prompt.
  #>
  #Requires -RunAsAdministrator

  $ErrorActionPreference = 'Stop'
  $dir      = Split-Path -Parent $MyInvocation.MyCommand.Path
  $msixPath = Join-Path $dir "Kneeboard.msix"
  $cerPath  = Join-Path $dir "Kneeboard.cer"

  foreach ($f in $msixPath, $cerPath) {
      if (-not (Test-Path $f)) {
          Write-Error "Required file not found: $f"
          exit 1
      }
  }

  # Trust the signing cert (idempotent)
  Write-Host "Checking certificate trust..."
  $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerPath)
  $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
      [System.Security.Cryptography.X509Certificates.StoreName]::TrustedPeople,
      [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine
  )
  $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
  $already = $store.Certificates.Find(
      [System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
      $cert.Thumbprint,
      $false
  )
  if ($already.Count -eq 0) {
      $store.Add($cert)
      Write-Host "Certificate imported to TrustedPeople."
  } else {
      Write-Host "Certificate already trusted."
  }
  $store.Close()

  # Install or upgrade
  Write-Host "Installing Kneeboard..."
  Add-AppxPackage -Path $msixPath -ForceApplicationShutdown

  Write-Host ""
  Write-Host "Done. Kneeboard is installed. Find it in the Start menu."
  Read-Host "Press Enter to close"
  ```

- [ ] **Step 2: Commit**

  ```powershell
  git add scripts/Install.ps1
  git commit -m "chore: add Install.ps1 recipient install script"
  ```

---

## Task 7: Create scripts/Publish-Release.ps1

**Files:**
- Create: `scripts/Publish-Release.ps1`

- [ ] **Step 1: Create the script**

  Create `scripts/Publish-Release.ps1`:

  ```powershell
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

  # Publish
  Write-Host "Running dotnet publish..."
  dotnet publish "$root\Kneeboard" `
      -f net10.0-windows10.0.19041.0 `
      -c Release `
      -p:PublishProfile=WinRelease
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

  # Locate the .msix (path includes version + arch, so search recursively)
  $searchRoot = Join-Path $root "Kneeboard\bin\Release\net10.0-windows10.0.19041.0"
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

  Copy-Item $msix.FullName           "$stage\Kneeboard.msix"
  Copy-Item "$root\Kneeboard.cer"    "$stage\Kneeboard.cer"
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
  ```

- [ ] **Step 2: Commit**

  ```powershell
  git add scripts/Publish-Release.ps1
  git commit -m "chore: add Publish-Release.ps1 release build helper"
  ```

---

## Task 8: End-to-end smoke test

- [ ] **Step 1: Run the full release build**

  ```powershell
  .\scripts\Publish-Release.ps1
  ```

  Expected: `Release artifact: ...\dist\Kneeboard-v1.0.zip` and the zip contains three files: `Kneeboard.msix`, `Kneeboard.cer`, `Install.ps1`.

- [ ] **Step 2: Install on the current machine to verify**

  Extract the zip to a temp folder, then:

  ```powershell
  # In the extracted folder, right-click Install.ps1 → Run with PowerShell
  # OR from an elevated prompt:
  powershell -ExecutionPolicy Bypass -File .\Install.ps1
  ```

  Expected: cert import message, "Installing Kneeboard...", "Done. Kneeboard is installed."

  Open the Start menu and verify Kneeboard appears and launches.

- [ ] **Step 3: Verify upgrade path**

  Re-run `Install.ps1` from the same zip. Expected: "Certificate already trusted." (no duplicate import), then installs over the existing version without error.

- [ ] **Step 4: Commit Kneeboard.cer if not already committed**

  ```powershell
  git status   # Kneeboard.cer should already be committed from Task 5
  ```

  If not yet committed:

  ```powershell
  git add Kneeboard.cer
  git commit -m "chore: commit public signing certificate"
  ```
