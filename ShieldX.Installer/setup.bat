@echo off
REM ShieldX Professional Antivirus Setup Launcher
REM This batch file launches the PowerShell-based installer

setlocal enabledelayedexpansion

REM Get the directory of this batch file
for %%I in ("%~dp0.") do set "SCRIPT_DIR=%%~fI"

REM Check if PowerShell is available
where /q powershell
if errorlevel 1 (
    echo [-] PowerShell is not installed or not in PATH
    pause
    exit /b 1
)

REM Launch PowerShell installer
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\setup.ps1" %*

endlocal
