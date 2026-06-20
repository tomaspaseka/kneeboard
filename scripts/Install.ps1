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

# Ensure WinAppRuntime 1.8 is present (required by the MSIX; not bundled in the package)
Write-Host "Checking Windows App Runtime..."
$winrt = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.1.8" -ErrorAction SilentlyContinue
if (-not $winrt) {
    Write-Host "Installing Windows App Runtime 1.8..."
    winget install --id Microsoft.WindowsAppRuntime.1.8 --silent --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install Windows App Runtime 1.8. Install it manually from https://aka.ms/windowsappsdk and re-run this script."
        exit 1
    }
} else {
    Write-Host "Windows App Runtime already installed ($($winrt.Version))."
}

# Install or upgrade
Write-Host "Installing Kneeboard..."
Add-AppxPackage -Path $msixPath -ForceApplicationShutdown

Write-Host ""
Write-Host "Done. Kneeboard is installed. Find it in the Start menu."
Read-Host "Press Enter to close"
