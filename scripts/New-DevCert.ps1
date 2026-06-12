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
$secPwd = if ($Password) {
    ConvertTo-SecureString -String $Password -Force -AsPlainText
} else {
    New-Object System.Security.SecureString
}
Export-PfxCertificate -Cert $cert -FilePath $pfxOut -Password $secPwd | Out-Null

# Export CER (public key — safe to commit and ship to recipients)
Export-Certificate -Cert $cert -FilePath $cerOut -Type CERT | Out-Null

Write-Host ""
Write-Host "Created: $pfxOut  (gitignored — keep this private)"
Write-Host "Created: $cerOut  (commit this; it goes in every release zip)"
Write-Host ""
Write-Host "Next: copy Kneeboard/Signing.props.example to Kneeboard/Signing.props"
Write-Host "      PackageCertificateKeyFile is already set to: $pfxOut"
if ($Password) {
    Write-Host "      and set PackageCertificatePassword = $Password"
}
