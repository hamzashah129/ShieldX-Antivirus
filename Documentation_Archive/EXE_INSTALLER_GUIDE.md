# ShieldX EXE Installer Creation Guide

## Overview

This guide explains how to create a professional Windows EXE installer (`ShieldX_Professional_Antivirus_v3.1.0_Setup.exe`) with a GUI that lets users choose between 32-bit and 64-bit installations.

## Prerequisites

### 1. NSIS (Nullsoft Scriptable Install System)
NSIS is a free, open-source tool that creates professional Windows installers.

**Download NSIS:**
- Website: https://nsis.sourceforge.io/
- Download: NSIS 3.x or later
- Installation: Run the installer and follow the wizard

**Installation Steps:**
1. Download NSIS from the website
2. Run the installer
3. Choose installation directory (default: `C:\Program Files (x86)\NSIS`)
4. Install full version with all plugins

### 2. MakeME Build Script (Recommended)
The NSIS installer script requires your builds to be ready in the correct structure.

**Build both architectures first:**
```batch
build-all-platforms.bat
```

This creates:
- `bin\Release\net8.0-windows\win-x64\ShieldX.exe`
- `bin\Release\net8.0-windows\win-x86\ShieldX.exe`

### 3. LICENSE.txt File
Create a LICENSE.txt file in the ShieldX root directory (or copy your existing one).

---

## Step 1: Prepare Your Build Files

```batch
REM 1. Build both architectures
build-all-platforms.bat

REM Expected output:
REM ✓ bin\Release\net8.0-windows\win-x64\
REM ✓ bin\Release\net8.0-windows\win-x86\
```

**Verify the builds exist:**
- ✅ `bin\Release\net8.0-windows\win-x64\ShieldX.exe`
- ✅ `bin\Release\net8.0-windows\win-x86\ShieldX.exe`

---

## Step 2: Verify the NSIS Script

The NSIS script file is: **`ShieldX_Installer.nsi`**

Key features in the script:
- ✅ Professional UI with Welcome page
- ✅ License agreement display
- ✅ Architecture selection dialog (32-bit/64-bit)
- ✅ Automatic file copying based on selection
- ✅ Shortcut creation
- ✅ Registry entries for Programs and Features
- ✅ Uninstall support

---

## Step 3: Compile the NSIS Script

### Option A: Using NSIS GUI (Easy)

1. **Open NSIS Compiler:**
   - Go to Start Menu → NSIS
   - Click "Makensis"

2. **Load the script:**
   - Click "Open Script"
   - Navigate to: `d:\ShieldX_Antivirus\ShieldX_Installer.nsi`
   - Click Open

3. **Compile:**
   - Click the "Compile NSI Scripts" button
   - The compiler will start building the EXE
   - Progress will show in the console

4. **Output:**
   - The EXE will be created at: `d:\ShieldX_Antivirus\ShieldX_Professional_Antivirus_v3.1.0_Setup.exe`
   - File size: ~100-150 MB (includes both architectures)

### Option B: Using Command Line

```batch
REM Run from ShieldX directory
cd d:\ShieldX_Antivirus

REM Compile using NSIS command-line
"C:\Program Files (x86)\NSIS\makensis.exe" ShieldX_Installer.nsi
```

Or create a batch file: `build-exe-installer.bat`

```batch
@echo off
echo Building ShieldX EXE Installer...
echo.

REM Check if NSIS is installed
if not exist "C:\Program Files (x86)\NSIS\makensis.exe" (
    echo ERROR: NSIS is not installed.
    echo Please download and install NSIS from: https://nsis.sourceforge.io/
    pause
    exit /b 1
)

REM Build the installer
cd /d "%~dp0"
"C:\Program Files (x86)\NSIS\makensis.exe" ShieldX_Installer.nsi

if errorlevel 1 (
    echo.
    echo ERROR: Failed to build installer.
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCCESS: EXE installer created!
echo ========================================
echo.
echo File: ShieldX_Professional_Antivirus_v3.1.0_Setup.exe
echo Location: %CD%
echo.
pause
```

### Option C: Using PowerShell Script

```powershell
# build-exe-installer.ps1
$NSISPath = "C:\Program Files (x86)\NSIS\makensis.exe"
$ScriptPath = "ShieldX_Installer.nsi"

if (-not (Test-Path $NSISPath)) {
    Write-Host "ERROR: NSIS not found at $NSISPath" -ForegroundColor Red
    Write-Host "Download from: https://nsis.sourceforge.io/" -ForegroundColor Yellow
    exit 1
}

Write-Host "Building EXE installer..." -ForegroundColor Cyan
& $NSISPath $ScriptPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: EXE installer created!" -ForegroundColor Green
    Get-Item "ShieldX_Professional_Antivirus_v3.1.0_Setup.exe" | Format-Table Name, @{Label="Size";Expression={"{0:N0} KB" -f ($_.Length/1024)}}
} else {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
```

---

## Step 4: Verify the EXE Was Created

After compilation, check for:
- ✅ `ShieldX_Professional_Antivirus_v3.1.0_Setup.exe` (main installer)
- ✅ File size: Approximately 100-150 MB
- ✅ Icon: Will have a Windows installer icon

**Verify in Windows Explorer:**
```
d:\ShieldX_Antivirus\ShieldX_Professional_Antivirus_v3.1.0_Setup.exe
```

---

## Step 5: Test the EXE Installer

### Test on a 64-bit System

1. **Run the EXE:**
   - Right-click `ShieldX_Professional_Antivirus_v3.1.0_Setup.exe`
   - Select "Run as Administrator"

2. **Welcome Page:**
   - "Welcome to ShieldX Professional Antivirus Setup"
   - Click "Next >"

3. **License Page:**
   - Review the license
   - Check "I Agree"
   - Click "Next >"

4. **Architecture Selection Page:**
   ```
   ┌─────────────────────────────────────┐
   │ Select Installation Architecture    │
   │                                     │
   │ ◉ 64-bit (x64) - Recommended       │
   │   • Better performance...           │
   │   • Supports more than 3GB RAM...  │
   │   • Recommended for Win 10/11...   │
   │                                     │
   │ ○ 32-bit (x86) - Legacy support    │
   │   • Compatible with legacy...      │
   │   • Lower memory footprint...      │
   │   • Works on both 32-bit/64-bit... │
   │                                     │
   │ [< Back] [Next >] [Cancel]         │
   └─────────────────────────────────────┘
   ```
   - Select preferred version
   - Click "Next >"

5. **Installation Progress:**
   - Shows progress as files are copied
   - Creates shortcuts
   - Updates Windows registry

6. **Finish Page:**
   - "ShieldX Professional Antivirus v3.1.0 has been installed successfully!"
   - Shows selected architecture: "Selected Architecture: x64-bit"
   - Click "Finish"

### Verify Installation

1. **Check Programs and Features:**
   - Settings → Apps → Installed apps
   - Find "ShieldX Professional Antivirus"
   - Note the installed architecture

2. **Check Shortcuts:**
   - Desktop: "ShieldX" shortcut should exist
   - Start Menu: Programs → ShieldX folder

3. **Check Installation Directory:**
   - `C:\Program Files\ShieldX Professional Antivirus\ShieldX.exe`
   - Contains all files for selected architecture

---

## What Each Installer Page Shows

### Welcome Page
```
┌─────────────────────────────────────┐
│ Welcome to Setup                    │
│                                     │
│ Welcome to the ShieldX Professional │
│ Antivirus v3.1.0 Setup wizard.     │
│                                     │
│ This wizard will guide you through  │
│ the installation process.           │
│                                     │
│ [Next >] [Cancel]                   │
└─────────────────────────────────────┘
```

### License Page
```
┌─────────────────────────────────────┐
│ License Agreement                   │
│                                     │
│ Please read the following license   │
│ agreement carefully:                │
│                                     │
│ [Large text area with LICENSE.txt]  │
│                                     │
│ ○ I do not agree                    │
│ ◉ I agree                           │
│                                     │
│ [< Back] [Next >] [Cancel]          │
└─────────────────────────────────────┘
```

### Architecture Selection Page (NEW)
```
┌─────────────────────────────────────┐
│ Select Installation Architecture    │
│                                     │
│ Choose which version to install on  │
│ your system:                        │
│                                     │
│ ◉ 64-bit (x64)                      │
│   • Better performance...           │
│                                     │
│ ○ 32-bit (x86)                      │
│   • Legacy system support...        │
│                                     │
│ [< Back] [Next >] [Cancel]          │
└─────────────────────────────────────┘
```

### Installation Progress Page
```
┌─────────────────────────────────────┐
│ Installing ShieldX Professional     │
│                                     │
│ [████████████████░░] 75%            │
│                                     │
│ Copying file: ShieldX.exe           │
│ Status: 127 MB of 170 MB            │
│                                     │
│ Installation completed successfully!│
│ Installed Architecture: x64-bit     │
│                                     │
│ [Cancel]                            │
└─────────────────────────────────────┘
```

### Finish Page
```
┌─────────────────────────────────────┐
│ Installation Complete               │
│                                     │
│ ShieldX Professional Antivirus      │
│ v3.1.0 has been installed          │
│ successfully!                       │
│                                     │
│ Selected Architecture: x64-bit      │
│                                     │
│ ☑ Launch ShieldX now               │
│                                     │
│ [Finish]                            │
└─────────────────────────────────────┘
```

---

## Troubleshooting EXE Creation

### NSIS Not Found Error

```
ERROR: NSIS is not installed.
```

**Solution:**
1. Download NSIS from https://nsis.sourceforge.io/
2. Run the NSIS installer
3. Select "Full Installation"
4. Verify path: `C:\Program Files (x86)\NSIS\makensis.exe`

### Build Files Not Found Error

```
ERROR: Build files not found for x86-bit at:
  bin\Release\net8.0-windows\win-x86\ShieldX.exe
```

**Solution:**
1. Build both architectures: `build-all-platforms.bat`
2. Verify output folders contain ShieldX.exe
3. Run NSIS compiler again

### LICENSE.txt Missing

```
Error reading license file
```

**Solution:**
1. Create or verify `LICENSE.txt` exists in root directory
2. Add license text to the file
3. Recompile the installer

### EXE File Size Too Large

```
File size is 500 MB instead of 150 MB
```

**Possible cause:** Unnecessary files being included

**Solution:**
- Edit ShieldX_Installer.nsi
- Check File inclusion lines
- Remove debug or temporary files

### Installation Directory Issues

**Error:** "Cannot write to Program Files"

**Solution:**
- Right-click EXE
- Select "Run as Administrator"
- Accept UAC prompt

---

## Distribution

Once the EXE is created:

### Option 1: Direct Distribution
- Share: `ShieldX_Professional_Antivirus_v3.1.0_Setup.exe`
- File size: ~120-150 MB
- Users double-click to install

### Option 2: Cloud Storage
- Upload to cloud service (OneDrive, Google Drive, etc.)
- Share download link
- Users download and run

### Option 3: Website Download
- Host on website
- Provide download link
- Add virus scan verification (VirusTotal)

---

## Command Reference

### Build Both Architectures
```batch
build-all-platforms.bat
```

### Compile NSIS to EXE
```batch
"C:\Program Files (x86)\NSIS\makensis.exe" ShieldX_Installer.nsi
```

### Quick Build + EXE (Batch Script)
```batch
build-all-platforms.bat && "C:\Program Files (x86)\NSIS\makensis.exe" ShieldX_Installer.nsi
```

---

## Version Information

- **Installer Type**: NSIS (Nullsoft Scriptable Install System)
- **Installer File**: ShieldX_Professional_Antivirus_v3.1.1_Setup.exe
- **ShieldX Version**: 3.1.1
- **Architectures**: x86 (32-bit), x64 (64-bit)
- **Supported Windows**: 7 SP1 and later
- **Estimated EXE Size**: 100-150 MB

---

## Next Steps

1. ✅ Install NSIS from https://nsis.sourceforge.io/
2. ✅ Build both architectures: `build-all-platforms.bat`
3. ✅ Verify build files exist
4. ✅ Compile NSIS script to create EXE
5. ✅ Test the installer on both systems
6. ✅ Distribute the EXE file

---

## Support

For NSIS help:
- Official guide: https://nsis.sourceforge.io/Docs/
- Script reference: https://nsis.sourceforge.io/Docs/Modern%20UI%202/Readme.html

For ShieldX installer issues:
- Check build output directories exist
- Verify NSIS installation
- Review NSIS compiler output for errors
