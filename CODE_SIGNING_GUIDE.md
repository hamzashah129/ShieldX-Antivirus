# ShieldX Code Signing Guide

**Version:** 3.1.1  
**Date:** April 26, 2026  
**Status:** Setup Complete ✅

---

## Overview

Code signing ensures users trust your installer by verifying its authenticity and integrity. A signed ShieldX installer will **NOT** display the "Unknown Publisher" warning.

---

## Implementation Status ✅

### Current Production Release: v3.1.1

| Component | Status | Details |
|-----------|--------|---------|
| **Self-Signed Certificate** | ✅ COMPLETE | Thumbprint: D27B8EEC9DFE10EBDFD3B7B1BAAD218E3304CCA9 |
| **Certificate Validity** | ✅ COMPLETE | 2026-04-26 → 2029-04-26 (3 years) |
| **ShieldX.exe Signed** | ✅ COMPLETE | SHA256 + Sectigo Timestamp |
| **Installer Package Signed** | ✅ COMPLETE | `ShieldX_Professional_v3.1.1_Setup.exe` |
| **Signature Timestamp** | ✅ COMPLETE | Sectigo Public Time Stamping (valid to 2036) |
| **Code Signing Automation** | ✅ COMPLETE | `build-installer.ps1 -SignInstaller` |
| **Certificate Management Script** | ✅ COMPLETE | `manage-certificates.ps1` (all actions) |

**Result:** Signed installer ready for distribution. Self-signed warnings appear only on systems that haven't imported the certificate.

---

## Table of Contents

1. [Implementation Status](#implementation-status-)
2. [Quick Start](#quick-start)
3. [Self-Signed Setup (Development)](#self-signed-setup-development)
4. [Production Certificate (Recommended)](#production-certificate-recommended)
5. [Building Signed Installers](#building-signed-installers)
6. [Verification](#verification)
7. [Certificate Management](#certificate-management)
8. [Troubleshooting](#troubleshooting)

---

## Quick Start

### To Sign a New Build

```powershell
cd D:\ShieldX_Antivirus

# Build and sign in one command
.\build-installer.ps1 -SignInstaller

# Output: ShieldX.Installer\output\ShieldX_Professional_v3.1.1_Setup.exe
```

### To Verify Existing Signature

```powershell
Get-AuthenticodeSignature "ShieldX.Installer\output\ShieldX_Professional_v3.1.1_Setup.exe" | 
    Format-List Status, SignerCertificate
```

---

## Self-Signed Setup (Development)

For testing and development purposes, use a self-signed certificate.

### Step 1: Generate Certificate

```powershell
cd D:\ShieldX_Antivirus
.\manage-certificates.ps1 -Action generate
```

**Output:**
```
========================================
   ShieldX Code Signing Certificate    
========================================

✓ Generated self-signed certificate
  Thumbprint: ABC123DEF456...
  Expires: 2029-04-26

✓ Exported certificate to: certificates\ShieldX_CodeSigning.pfx
  Password: ShieldX@2026

✓ Exported public cert to: certificates\ShieldX_CodeSigning.cer
```

**Certificate Location:**
- Private Key: `certificates\ShieldX_CodeSigning.pfx`
- Public Key: `certificates\ShieldX_CodeSigning.cer`
- Password: `ShieldX@2026`

### Step 2: Build Signed Installer

```powershell
# Auto-detect certificate and sign
.\build-installer.ps1 -SignInstaller

# Or with explicit certificate path
.\build-installer.ps1 `
    -CertPath "certificates\ShieldX_CodeSigning.pfx" `
    -CertPassword "ShieldX@2026" `
    -SignInstaller
```

### Step 3: Verify Signature

```powershell
.\manage-certificates.ps1 -Action verify `
    -FilePath "ShieldX.Installer\output\ShieldX_Professional_v3.1.1_Setup.exe"
```

---

## Production Certificate (Recommended)

For production distribution, obtain a code signing certificate from a trusted Certificate Authority (CA).

### Supported CAs

| Provider | Cost | Validation | Notes |
|----------|------|-----------|-------|
| **DigiCert** | $299/year | OV/EV | Industry standard, fast |
| **Sectigo** | $149/year | OV | Good value, trusted |
| **GlobalSign** | $299/year | OV/EV | Enterprise grade |
| **Comodo** | $149/year | OV | Budget-friendly |

### Steps to Obtain

1. **Generate Certificate Signing Request (CSR)**
   ```powershell
   # Using New-SelfSignedCertificate with CSR generation
   # Contact your IT department or CA for CSR generation
   ```

2. **Submit to CA**
   - Verify your identity
   - Pay subscription fee
   - Receive signed certificate

3. **Install Certificate**
   ```powershell
   # Import the .pfx from CA into certificates\
   Copy-Item "path\to\ca-certificate.pfx" "certificates\ShieldX_Production.pfx"
   ```

4. **Build Signed Installer**
   ```powershell
   .\build-installer.ps1 `
       -CertPath "certificates\ShieldX_Production.pfx" `
       -CertPassword "your-password" `
       -SignInstaller
   ```

---

## Building Signed Installers

### Method 1: Auto-Detection (Easiest)

```powershell
.\build-installer.ps1 -SignInstaller
```

**Behavior:**
- Automatically finds certificate in `certificates\` folder
- Uses password `ShieldX@2026` for self-signed certs
- Proceeds without signing if certificate not found

### Method 2: Explicit Parameters

```powershell
.\build-installer.ps1 `
    -Version "3.1.1" `
    -CertPath "certificates\ShieldX_CodeSigning.pfx" `
    -CertPassword "ShieldX@2026" `
    -SignInstaller
```

### Method 3: Pipeline Integration

In CI/CD (GitHub Actions, Azure DevOps):

```yaml
# GitHub Actions example
- name: Sign Installer
  run: |
    .\build-installer.ps1 `
      -CertPath "${{ secrets.CERT_PATH }}" `
      -CertPassword "${{ secrets.CERT_PASSWORD }}" `
      -SignInstaller
```

---

## Verification

### Check Installer Signature

```powershell
# Simple check
(Get-AuthenticodeSignature "path\to\Setup.exe").Status

# Detailed info
Get-AuthenticodeSignature "ShieldX.Installer\output\ShieldX_Professional_v3.1.1_Setup.exe" | 
    Format-List SignerCertificate, Status, TimeStamperCertificate
```

**Expected Output (Signed):**
```
SignerCertificate      : [Subject=CN=SYED HAMZA ALI SHAH...]
Status                 : Valid
TimeStamperCertificate : [Subject=CN=...]
```

**Expected Output (Unsigned):**
```
Status : NotSigned
```

### Verify with Tools

```powershell
# Using manage-certificates.ps1
.\manage-certificates.ps1 -Action verify `
    -FilePath "ShieldX.Installer\output\ShieldX_Professional_v3.1.1_Setup.exe"

# Using Windows Explorer (Optional)
# Right-click Setup.exe → Properties → Digital Signatures
```

---

## Certificate Management

### View Certificate Information

```powershell
# List all certificates
.\manage-certificates.ps1 -Action info

# View specific certificate
.\manage-certificates.ps1 -Action info `
    -CertPath "certificates\ShieldX_CodeSigning.pfx" `
    -CertPassword "ShieldX@2026"
```

### Export Public Certificate

For distribution to end-users (optional):

```powershell
# Users can import to trust your certificates
# File: certificates\ShieldX_CodeSigning.cer

# To distribute:
# 1. Copy .cer file to website
# 2. Provide download link in README
# 3. Users run: certmgr.msc → import to Trusted Publishers
```

### Renew Certificate

Self-signed certificates expire after 3 years:

```powershell
# Before expiration, regenerate
.\manage-certificates.ps1 -Action generate
```

---

## Timestamping

**What is it?** Timestamping ensures signatures remain valid after the certificate expires.

**Why use it?** Without timestamping, installers become "invalid" once certificate expires.

### Supported Timestamp Servers

- **Sectigo:** `http://timestamp.sectigo.com` (Default)
- **DigiCert:** `http://timestamp.digicert.com`
- **GlobalSign:** `http://timestamp.globalsign.com/scripts/timstamp.dll`

**Configuration:** Edit `manage-certificates.ps1` line ~180:

```powershell
$TimeStampUrl = "http://timestamp.sectigo.com"
```

---

## Troubleshooting

### ❌ SignTool Not Found

**Error:**
```
✗ SignTool.exe not found. Install Windows SDK.
```

**Solution:**

1. **Option A: Install Windows SDK**
   - Download: https://developer.microsoft.com/windows/downloads/windows-sdk/
   - Install "Windows App SDK" component
   - Restart PowerShell

2. **Option B: Manually set SignTool path**
   - Edit `manage-certificates.ps1`
   - Update `$signToolPath` variable with actual path

3. **Option C: Use GitHub Actions**
   - SignTool is pre-installed on GitHub runners

### ❌ Certificate Not Found

**Error:**
```
✗ Certificate not found: certificates\ShieldX_CodeSigning.pfx
```

**Solution:**
```powershell
# Generate certificate first
.\manage-certificates.ps1 -Action generate

# Verify it exists
dir certificates\*.pfx
```

### ❌ Wrong Password

**Error:**
```
ERROR: The password for the PFX file was not specified correctly.
```

**Solution:**
```powershell
# Check password matches
.\manage-certificates.ps1 -Action info `
    -CertPath "certificates\ShieldX_CodeSigning.pfx" `
    -CertPassword "ShieldX@2026"

# If it fails, certificate may be corrupted - regenerate
.\manage-certificates.ps1 -Action generate
```

### ⚠️ Timestamp Server Unavailable

**Warning:**
```
The timestamp service could not be contacted
```

**Solution:**
- Temporary issue (timestamp server down)
- Signing still completes, but without timestamp
- Try again later, or use different timestamp server
- Timestamp is optional (recommended but not required)

### ❌ Signature Not Valid

**Error:**
```
Status : Invalid
```

**Solution:**
- Certificate may be corrupted
- Password incorrect
- Try regenerating:
  ```powershell
  .\manage-certificates.ps1 -Action generate
  .\build-installer.ps1 -SignInstaller
  ```

---

## Best Practices

✅ **DO:**
- Use production certificates for distribution
- Protect certificate files and passwords
- Store certificates in version control (encrypted)
- Use timestamping on all production builds
- Renew certificates before expiration
- Test installation on clean Windows machines

❌ **DON'T:**
- Share certificate passwords via email
- Commit certificate files to public repos
- Use self-signed certs in production
- Ignore expiration warnings
- Mix multiple certificates in one build

---

## Security Recommendations

### Certificate Storage

```powershell
# Encrypt certificate password in your CI/CD
# GitHub Secrets example:
$CertPassword = $env:CERT_PASSWORD  # From secrets

# Azure Key Vault example:
Get-AzKeyVaultSecret -VaultName "ShieldX" -Name "CertPassword"
```

### Access Control

- Restrict who can build signed installers
- Audit certificate usage
- Rotate keys annually
- Use MFA for certificate access

### Monitoring

```powershell
# Check certificate expiration
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$cert.Import("certificates\ShieldX_CodeSigning.pfx", "password", 0)
$daysLeft = ($cert.NotAfter - (Get-Date)).Days
Write-Host "Certificate expires in $daysLeft days"
```

---

## References

- **Windows SDK:** https://developer.microsoft.com/windows/downloads/windows-sdk/
- **SignTool Docs:** https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool
- **Code Signing Guide:** https://learn.microsoft.com/en-us/windows/win32/seccrypto/using-signtool-to-sign-a-file
- **EV Certificates:** https://learn.microsoft.com/en-us/windows/win32/seccrypto/ev-codesigning-and-timestamps

---

## Support

For issues:
1. Check **Troubleshooting** section above
2. Review certificate password and paths
3. Verify Windows SDK installed
4. Test on different network (if timestamp issue)

---

**Last Updated:** April 26, 2026  
**Maintained By:** ShieldX Development Team
