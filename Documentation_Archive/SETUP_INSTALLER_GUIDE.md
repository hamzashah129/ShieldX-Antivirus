# ShieldX Setup Installer Guide

## Overview

The enhanced ShieldX Setup Installer now features an intuitive GUI for selecting between 32-bit and 64-bit installations. Users can easily choose which version to install on their system.

## Installation Methods

### Method 1: Using the Setup Batch File (Recommended)

The easiest way to install ShieldX is to use the setup batch file:

1. **Locate the installer:**
   - Find `ShieldX_Setup.bat` in the ShieldX directory

2. **Run as Administrator:**
   - Right-click on `ShieldX_Setup.bat`
   - Select **"Run as administrator"**

3. **Architecture Selection Dialog:**
   - A dialog window will appear showing available versions
   - Select your preferred installation:
     - **64-bit (x64)**: Recommended for modern systems with 64-bit Windows
     - **32-bit (x86)**: For legacy systems or compatibility

4. **Click Install:**
   - Select your version and click the **"Install"** button
   - The installer will proceed with the selected architecture

### Method 2: PowerShell Installer (Direct)

For advanced users, you can run the PowerShell script directly:

```powershell
# Run as Administrator PowerShell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\ShieldX_Installer.ps1
```

### Method 3: Command Line with Specific Architecture

To specify an architecture directly without the dialog:

```powershell
# Install 64-bit version
.\ShieldX_Installer.ps1 -Architecture x64

# Install 32-bit version
.\ShieldX_Installer.ps1 -Architecture x86
```

## Architecture Selection Dialog

When you run the installer with both architectures available, you'll see a dialog like this:

```
╔═══════════════════════════════════════════════════════════╗
║  ShieldX Professional Antivirus v3.1.0                    ║
║  Architecture Selection                                   ║
╠═══════════════════════════════════════════════════════════╣
║                                                           ║
║  Select Installation Version                             ║
║                                                           ║
║  ( ) 64-bit (x64) - Recommended for modern systems       ║
║        • Supports systems with more than 3GB RAM         ║
║        • Modern 64-bit processors                        ║
║        • Better performance on 64-bit Windows            ║
║                                                           ║
║  ( ) 32-bit (x86) - Legacy system support                ║
║        • Compatible with legacy systems                  ║
║        • Lower memory footprint                          ║
║        • Works on both 32-bit and 64-bit Windows        ║
║                                                           ║
║  [Cancel]              [Install]                         ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
```

## What Each Version Offers

### 64-bit (x64)
- **Best for:** Modern systems with 64-bit Windows
- **Features:**
  - Supports more than 3GB of RAM
  - Better performance on 64-bit processors
  - Optimized for contemporary Windows versions (Windows 10/11)
  - Recommended for most users
- **Requirements:**
  - Windows Vista or later (64-bit)
  - .NET 8.0 Runtime (64-bit)
  - 512 MB RAM minimum

### 32-bit (x86)
- **Best for:** Legacy systems or compatibility needs
- **Features:**
  - Works on both 32-bit and 64-bit Windows
  - Lower memory footprint
  - Compatible with older systems
  - Fallback option for special cases
- **Requirements:**
  - Windows 7 SP1 or later (32-bit or 64-bit)
  - .NET 8.0 Runtime (32-bit)
  - 256 MB RAM minimum

## Installation Process

Once you select your version, the installer will:

1. **Create Installation Directory**
   - Creates `C:\Program Files\ShieldX Professional Antivirus`

2. **Copy Application Files**
   - Copies all necessary files for your selected architecture

3. **Create Shortcuts**
   - Desktop shortcut for easy access
   - Start Menu folder with launch shortcut

4. **Register with Windows**
   - Adds ShieldX to "Programs and Features"
   - Enables standard Windows uninstall

5. **Launch Application**
   - Automatically starts ShieldX after installation

## Troubleshooting

### Dialog Doesn't Appear

If the architecture selection dialog doesn't appear, it means:
- Only one architecture build is available
- The installer will automatically use the available version

**Solution:** Build both architectures first:
```powershell
dotnet publish -c Release -p:Platform=x64
dotnet publish -c Release -p:Platform=x86
```

### "Run as Administrator" Issues

**Error:** "Access Denied" or "Permission Denied"

**Solution:**
1. Right-click the batch file
2. Select **"Run as administrator"**
3. Click **"Yes"** when prompted by User Account Control (UAC)

### Builds Not Found

**Error:** "Build files not found"

**Solution:**
1. Navigate to the ShieldX directory
2. Run the build script:
   ```batch
   build-all-platforms.bat
   ```
   Or manually:
   ```powershell
   dotnet publish -c Release -p:Platform=x64
   dotnet publish -c Release -p:Platform=x86
   ```

### PowerShell Execution Policy

**Error:** "Running scripts is disabled on this system"

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser
```

## Uninstallation

### Using Programs and Features

1. Open **Settings** → **Apps** → **Installed apps**
2. Find "ShieldX Professional Antivirus"
3. Click **Uninstall**
4. Confirm the uninstallation

### Using Command Line

```powershell
# Run as Administrator
.\ShieldX_Installer.ps1 -Uninstall
```

## System Detection

The installer includes smart system detection:

- Automatically detects your Windows bit version (32-bit or 64-bit)
- Shows the recommended version by default
- Allows override if you want to force a specific architecture
- Saves the installed architecture in Windows Registry for future reference

## Frequently Asked Questions

### Q: Which version should I choose?

**A:** For most users, choose **64-bit (x64)** if:
- Your Windows is 64-bit (Windows 10/11 64-bit)
- You have modern hardware
- You're unsure

Choose **32-bit (x86)** if:
- Your Windows is 32-bit
- You have an older system
- You specifically need legacy compatibility

### Q: Can I change versions later?

**A:** Yes, uninstall the current version and run the installer again to select a different architecture.

### Q: Does 32-bit version work on 64-bit Windows?

**A:** Yes, the 32-bit version is fully compatible with 64-bit Windows systems. However, 64-bit is usually preferred for better performance.

### Q: What if both versions fail to install?

**A:** Make sure:
1. You're running as Administrator
2. The build files exist in the correct directory
3. You have adequate disk space (minimum 100 MB)
4. .NET 8.0 Runtime is installed
5. No other installation is in progress

## Advanced Options

### Command Line Parameters

```powershell
# Uninstall mode
.\ShieldX_Installer.ps1 -Uninstall

# Specify architecture (skip dialog)
.\ShieldX_Installer.ps1 -Architecture x64
.\ShieldX_Installer.ps1 -Architecture x86
```

### Custom Installation Directory

To modify the installation directory, edit `ShieldX_Installer.ps1`:
```powershell
$InstallPath = "C:\Program Files\ShieldX Professional Antivirus"
```

## Version Information

- **Application:** ShieldX Professional Antivirus
- **Version:** 3.1.0
- **.NET Framework:** .NET 8.0
- **Supported Architectures:** x86 (32-bit), x64 (64-bit)
- **Supported Platforms:** Windows 7 SP1 and later

## Need Help?

If you encounter issues:
1. Check this guide's troubleshooting section
2. Verify both builds are available
3. Run the installer with administrator privileges
4. Check Windows Event Viewer for system errors
5. Review the installation logs in `%LOCALAPPDATA%\ShieldX\logs\`

