@echo off
REM ShieldX Professional Antivirus - Setup Installer Wrapper
REM This batch file provides a user-friendly entry point for the installer

setlocal enabledelayedexpansion

REM Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo ========================================
    echo ShieldX Setup Installer
    echo ========================================
    echo.
    echo ERROR: This installer requires administrator privileges.
    echo.
    echo Please right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Check if PowerShell is available
where powershell >nul 2>nul
if errorlevel 1 (
    echo ERROR: PowerShell is not available on this system.
    echo PowerShell is required to run the installer.
    pause
    exit /b 1
)

echo.
echo ========================================
echo ShieldX Professional Antivirus v3.1.1
echo Setup Installer
echo ========================================
echo.
echo Loading installer...
echo.

REM Get the directory where this batch file is located
set SCRIPT_DIR=%~dp0

REM Run the PowerShell installer with elevation
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%ShieldX_Installer.ps1"

if errorlevel 1 (
    echo.
    echo Installation failed. Press Enter to exit...
    pause
    exit /b 1
)

exit /b 0
