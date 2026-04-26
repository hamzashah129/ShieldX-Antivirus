param(
    [ValidateSet("generate", "sign", "verify", "info")]
    [string]$Action = "info",
    [string]$CertPath,
    [string]$CertPassword,
    [string]$FilePath
)

<#
.SYNOPSIS
    Code Signing Certificate Manager for ShieldX Antivirus
    
.DESCRIPTION
    Manages code signing certificates and signs executables/installers.
    Supports both self-signed certificates (testing) and production certificates.

.PARAMETER Action
    - generate: Create a self-signed certificate
    - sign: Sign an executable or installer
    - verify: Verify a signed file
    - info: Display certificate information

.EXAMPLE
    # Generate a self-signed certificate
    .\manage-certificates.ps1 -Action generate

    # Sign an installer
    .\manage-certificates.ps1 -Action sign -CertPath "certs\ShieldX.pfx" -CertPassword "password" -FilePath "installer.exe"

    # Verify a signature
    .\manage-certificates.ps1 -Action verify -FilePath "installer.exe"

    # Display certificate info
    .\manage-certificates.ps1 -Action info -CertPath "certs\ShieldX.pfx" -CertPassword "password"
#>

function New-SelfSignedCert {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   ShieldX Code Signing Certificate    " -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $certDir = "certificates"
    if (-not (Test-Path $certDir)) {
        New-Item $certDir -ItemType Directory -Force | Out-Null
        Write-Host "[+] Created certificate directory: $certDir" -ForegroundColor Green
    }

    # Generate self-signed certificate
    $certName = "ShieldX_CodeSigning"
    $certPath = "$certDir\$certName.cer"
    $pfxPath = "$certDir\$certName.pfx"
    
    $password = ConvertTo-SecureString "ShieldX@2026" -AsPlainText -Force

    try {
        $cert = New-SelfSignedCertificate `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -Subject "CN=SYED HAMZA ALI SHAH, OU=ShieldX, O=ShieldX Antivirus, C=US" `
            -KeyUsage DigitalSignature `
            -Type CodeSigningCert `
            -FriendlyName "ShieldX Code Signing Certificate" `
            -NotAfter (Get-Date).AddYears(3) `
            -KeyLength 2048 `
            -ErrorAction Stop

        Write-Host "[+] Generated self-signed certificate" -ForegroundColor Green
        Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor White
        Write-Host "  Expires: $($cert.NotAfter)" -ForegroundColor White
        Write-Host ""

        # Export to PFX
        Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password -Force | Out-Null
        Write-Host "[+] Exported certificate to: $pfxPath" -ForegroundColor Green
        Write-Host "  Password: ShieldX@2026" -ForegroundColor Yellow
        Write-Host ""

        # Export public certificate
        Export-Certificate -Cert $cert -FilePath $certPath -Type CERT -Force | Out-Null
        Write-Host "[+] Exported public cert to: $certPath" -ForegroundColor Green
        Write-Host ""

        Write-Host "Certificate Setup Complete!" -ForegroundColor Green
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "  1. Use this certificate to sign installers"
        Write-Host "  2. For production: Obtain a code signing certificate from a CA" -ForegroundColor Yellow
        Write-Host "  3. Users can install the public certificate ($certPath) to trust"
        Write-Host ""
        
        return @{
            PfxPath = $pfxPath
            CerPath = $certPath
            Thumbprint = $cert.Thumbprint
            Password = "ShieldX@2026"
        }
    }
    catch {
        Write-Host "[-] Error creating certificate: $_" -ForegroundColor Red
        return $null
    }
}

function Sign-File {
    param(
        [Parameter(Mandatory=$true)]
        [string]$FilePath,
        
        [Parameter(Mandatory=$true)]
        [string]$CertPath,
        
        [Parameter(Mandatory=$true)]
        [string]$Password,
        
        [string]$TimeStampUrl = "http://timestamp.sectigo.com"
    )

    if (-not (Test-Path $FilePath)) {
        Write-Host "[-] File not found: $FilePath" -ForegroundColor Red
        return $false
    }

    if (-not (Test-Path $CertPath)) {
        Write-Host "[-] Certificate not found: $CertPath" -ForegroundColor Red
        return $false
    }

    try {
        # Check if SignTool.exe is available
        $signToolPath = "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
        if (-not (Test-Path $signToolPath)) {
            $signToolPath = "C:\Program Files (x86)\Windows Kits\11\bin\x64\signtool.exe"
        }

        if (-not (Test-Path $signToolPath)) {
            Write-Host "[-] SignTool.exe not found. Install Windows SDK." -ForegroundColor Red
            Write-Host "  Download: https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Yellow
            return $false
        }

        Write-Host "Signing: $FilePath" -ForegroundColor Cyan
        Write-Host "Certificate: $CertPath" -ForegroundColor White

        # Sign the file
        $arguments = @(
            "sign",
            "/f", "`"$CertPath`"",
            "/p", "`"$Password`"",
            "/t", $TimeStampUrl,
            "/fd", "SHA256",
            "`"$FilePath`""
        )

        $output = & $signToolPath $arguments 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[+] File signed successfully!" -ForegroundColor Green
            Write-Host ""
            
            # Verify signature
            $verifyArgs = @("verify", "/pa", "`"$FilePath`"")
            $verifyOutput = & $signToolPath $verifyArgs 2>&1
            Write-Host $verifyOutput -ForegroundColor White
            
            return $true
        }
        else {
            Write-Host "[-] Signing failed:" -ForegroundColor Red
            Write-Host $output -ForegroundColor Yellow
            return $false
        }
    }
    catch {
        Write-Host "[-] Error signing file: $_" -ForegroundColor Red
        return $false
    }
}

function Verify-Signature {
    param([string]$FilePath)

    if (-not (Test-Path $FilePath)) {
        Write-Host "[-] File not found: $FilePath" -ForegroundColor Red
        return $false
    }

    try {
        $signToolPath = "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
        if (-not (Test-Path $signToolPath)) {
            $signToolPath = "C:\Program Files (x86)\Windows Kits\11\bin\x64\signtool.exe"
        }

        if (-not (Test-Path $signToolPath)) {
            Write-Host "SignTool not found. Cannot verify." -ForegroundColor Yellow
            return $false
        }

        Write-Host "Verifying: $FilePath" -ForegroundColor Cyan
        $output = & $signToolPath verify /pa "`"$FilePath`"" 2>&1
        
        Write-Host $output -ForegroundColor White
        return $LASTEXITCODE -eq 0
    }
    catch {
        Write-Host "[-] Error verifying signature: $_" -ForegroundColor Red
        return $false
    }
}

function Show-CertInfo {
    param(
        [string]$CertPath,
        [string]$Password
    )

    if (-not (Test-Path $CertPath)) {
        Write-Host "[-] Certificate not found: $CertPath" -ForegroundColor Red
        return
    }

    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
        $cert.Import($CertPath, $Password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::DefaultKeySet)

        Write-Host "Certificate Information:" -ForegroundColor Cyan
        Write-Host "  Subject: $($cert.Subject)" -ForegroundColor White
        Write-Host "  Issuer: $($cert.Issuer)" -ForegroundColor White
        Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor White
        Write-Host "  NotBefore: $($cert.NotBefore)" -ForegroundColor White
        Write-Host "  NotAfter: $($cert.NotAfter)" -ForegroundColor White
        Write-Host "  Public Key: $($cert.PublicKey.Key.KeySize) bits" -ForegroundColor White
    }
    catch {
        Write-Host "[-] Error reading certificate: $_" -ForegroundColor Red
    }
}

# Main execution
switch ($Action) {
    "generate" {
        New-SelfSignedCert
    }
    "sign" {
        if (-not $CertPath -or -not $FilePath -or -not $CertPassword) {
            Write-Host "[-] sign action requires: -CertPath, -CertPassword, -FilePath" -ForegroundColor Red
            exit 1
        }
        Sign-File -FilePath $FilePath -CertPath $CertPath -Password $CertPassword
    }
    "verify" {
        if (-not $FilePath) {
            Write-Host "[-] verify action requires: -FilePath" -ForegroundColor Red
            exit 1
        }
        Verify-Signature -FilePath $FilePath
    }
    "info" {
        if (-not $CertPath) {
            Write-Host "Available certificates:" -ForegroundColor Cyan
            Get-ChildItem "certificates\*.pfx" -ErrorAction SilentlyContinue | ForEach-Object {
                Write-Host "  - $($_.Name)" -ForegroundColor White
            }
        }
        else {
            if (-not $CertPassword) {
                Write-Host "[!] Certificate password not provided. Skipping detailed info." -ForegroundColor Yellow
            }
            else {
                Show-CertInfo -CertPath $CertPath -Password $CertPassword
            }
        }
    }
    default {
        Write-Host "ShieldX Code Signing Manager" -ForegroundColor Cyan
        Write-Host "Usage:" -ForegroundColor White
        Write-Host "  .\manage-certificates.ps1 -Action generate                          # Create self-signed cert"
        Write-Host "  .\manage-certificates.ps1 -Action sign -CertPath ... -FilePath ...  # Sign a file"
        Write-Host "  .\manage-certificates.ps1 -Action verify -FilePath ...              # Verify signature"
        Write-Host "  .\manage-certificates.ps1 -Action info [-CertPath ...]              # Show cert info"
        Write-Host ""
    }
}
