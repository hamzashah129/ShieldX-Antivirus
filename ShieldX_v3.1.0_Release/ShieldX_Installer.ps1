# ShieldX Professional Antivirus v3.1.0 Installer
param([switch]$Uninstall)
$InstallPath = "C:\Program Files\ShieldX Professional Antivirus"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ScriptDir) { $ScriptDir = Get-Location }
$BuildPath = $ScriptDir
$AppName = "ShieldX.exe"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Installer requires administrator privileges. Please run as Administrator." -ForegroundColor Red; exit 1
}

if ($Uninstall) {
    Write-Host "Uninstalling..." -ForegroundColor Yellow
    if (Test-Path "$env:USERPROFILE\Desktop\ShieldX.lnk") { Remove-Item "$env:USERPROFILE\Desktop\ShieldX.lnk" -Force }
    if (Test-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ShieldX") { Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ShieldX" -Recurse -Force }
    if (Test-Path $InstallPath) { Remove-Item $InstallPath -Recurse -Force }
    if (Test-Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX") { Remove-Item "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX" -Force }
    Write-Host "Uninstalled!" -ForegroundColor Green; Read-Host "`nPress Enter to exit"
    exit
}

Write-Host "ShieldX Professional Antivirus v3.1.0 Installer" -ForegroundColor Cyan
if (-not (Test-Path "$BuildPath\$AppName")) { Write-Host "ERROR: ShieldX.exe not found in $BuildPath" -ForegroundColor Red; exit 1 }

Write-Host "[1/4] Creating installation directory..." -ForegroundColor Yellow
if (-not (Test-Path $InstallPath)) { New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null }
Write-Host "[2/4] Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$BuildPath\*" -Destination $InstallPath -Recurse -Force

Write-Host "[3/4] Creating shortcuts..." -ForegroundColor Yellow
$WshShell = New-Object -ComObject WScript.Shell
$DesktopLnk = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\ShieldX.lnk")
$DesktopLnk.TargetPath = "$InstallPath\ShieldX.exe"
$DesktopLnk.WorkingDirectory = $InstallPath
$DesktopLnk.Save()

md "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ShieldX" -ErrorAction SilentlyContinue | Out-Null
$StartMenuLnk = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ShieldX\ShieldX.lnk")
$StartMenuLnk.TargetPath = "$InstallPath\ShieldX.exe"
$StartMenuLnk.WorkingDirectory = $InstallPath
$StartMenuLnk.Save()

Write-Host "[4/4] Registering..." -ForegroundColor Yellow
$RegPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX"
if (-not (Test-Path $RegPath)) { New-Item -Path $RegPath -Force | Out-Null }
Set-ItemProperty -Path $RegPath -Name "DisplayName" -Value "ShieldX Professional Antivirus"
Set-ItemProperty -Path $RegPath -Name "DisplayVersion" -Value "3.1.0"
Set-ItemProperty -Path $RegPath -Name "InstallLocation" -Value $InstallPath

Write-Host "`n? Installation completed!" -ForegroundColor Green
Write-Host "Starting ShieldX..." -ForegroundColor Yellow
Start-Process -FilePath "$InstallPath\$AppName"
Read-Host "`nPress Enter to exit"
