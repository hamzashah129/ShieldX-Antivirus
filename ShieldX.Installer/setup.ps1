# ShieldX Professional Antivirus Setup Script
# Self-contained installer for ShieldX

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    $Args
)

$ErrorActionPreference = 'Stop'

# Get the directory where the installer script is located
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$payloadPath = Join-Path $scriptDir "ShieldX.zip"
$installDir = Join-Path $env:ProgramFiles "ShieldX"

Write-Host "========================================" -ForegroundColor Green
Write-Host " ShieldX Professional Antivirus v3.1.1" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Check if running as Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "[!] This installer requires Administrator privileges." -ForegroundColor Red
    Write-Host "[+] Requesting elevation..." -ForegroundColor Yellow
    
    # Restart as Administrator
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`" $Args"
    Start-Process -FilePath PowerShell.exe -ArgumentList $arguments -Verb RunAs
    exit
}

# Create installation directory
Write-Host "[1/4] Creating installation directory..." -ForegroundColor Cyan
if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force -ea SilentlyContinue
}
New-Item -ItemType Directory -Path $installDir -Force | Out-Null

# Extract payload
Write-Host "[2/4] Extracting application files..." -ForegroundColor Cyan
if (Test-Path $payloadPath) {
    Expand-Archive -Path $payloadPath -DestinationPath $installDir -Force
    Write-Host "[+] Extracted successfully" -ForegroundColor Green
} else {
    Write-Host "[-] Payload ZIP not found at $payloadPath" -ForegroundColor Red
    exit 1
}

# Create shortcuts
Write-Host "[3/4] Creating shortcuts..." -ForegroundColor Cyan
$desktopPath = [Environment]::GetFolderPath("Desktop")
$startMenuPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\ShieldX"
$exePath = Join-Path $installDir "ShieldX.exe"

New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null

# Desktop shortcut
$shell = New-Object -COM WScript.Shell
$link = $shell.CreateShortCut("$desktopPath\ShieldX.lnk")
$link.TargetPath = $exePath
$link.Save()

# Start Menu shortcut
$link = $shell.CreateShortCut("$startMenuPath\ShieldX.lnk")
$link.TargetPath = $exePath
$link.Save()

Write-Host "[+] Shortcuts created" -ForegroundColor Green

# Registry entries
Write-Host "[4/4] Registering application..." -ForegroundColor Cyan
$regPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX"
if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}

Set-ItemProperty -Path $regPath -Name "DisplayName" -Value "ShieldX Professional Antivirus"
Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value "3.1.1"
Set-ItemProperty -Path $regPath -Name "Publisher" -Value "SYED HAMZA ALI SHAH"
Set-ItemProperty -Path $regPath -Name "UninstallString" -Value "`"$exePath`" --uninstall"
Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $installDir

Write-Host "[+] Registration complete" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " INSTALLATION SUCCESSFUL" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "ShieldX has been installed to: $installDir" -ForegroundColor Yellow
Write-Host ""

# Optional: Launch the application
$response = Read-Host "Launch ShieldX now? (Y/n)"
if ($response -ne "n") {
    & $exePath
}
