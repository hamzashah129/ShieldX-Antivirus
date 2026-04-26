@echo off
REM ShieldX Professional Antivirus v3.1.0 Installer
REM This batch file launches the PowerShell installer

setlocal enabledelayedexpansion

REM Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ShieldX requires administrator privileges.
    echo Please right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Get the script directory
set "SCRIPT_DIR=%~dp0"

REM Run the PowerShell installer
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%ShieldX_Installer.ps1"

exit /b 0
