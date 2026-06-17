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
