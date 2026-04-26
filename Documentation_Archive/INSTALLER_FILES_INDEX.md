# ShieldX Installer Files - Quick Reference

## 🚀 START HERE: For End Users

**Want to install ShieldX?**
→ Double-click or run: **`ShieldX_Setup.bat`**

Then choose your architecture (32-bit or 64-bit) from the dialog that appears.

---

## 📂 Installer Files Overview

### Main Installer Files

| File | Click to | Use When |
|------|----------|----------|
| **ShieldX_Setup.bat** | Double-click | You want the easiest installation (RECOMMENDED) |
| **ShieldX_Installer.ps1** | Run in PowerShell | You prefer command-line setup |

### Documentation Files

| File | Purpose |
|------|---------|
| **QUICK_START_INSTALL.md** | 1-page quick reference for installation |
| **SETUP_INSTALLER_GUIDE.md** | Complete detailed guide (200+ lines) |
| **GUI_SETUP_INSTALLER_SUMMARY.md** | Technical overview of the GUI setup |
| **MULTI_PLATFORM_BUILD_GUIDE.md** | How to build for 32-bit & 64-bit |

### Build Helper Files

| File | Use For |
|------|---------|
| **build-all-platforms.bat** | Build both 32-bit and 64-bit versions automatically |
| **build-installer.ps1** | PowerShell build script (alternative) |

---

## 🎯 Common Tasks

### I want to install ShieldX
1. Run: **`ShieldX_Setup.bat`**
2. Choose your version (64-bit or 32-bit)
3. Click Install

### I need to build both versions first
1. Run: **`build-all-platforms.bat`**
2. Wait for build to complete
3. Then run `ShieldX_Setup.bat`

### I want command-line installation
```powershell
.\ShieldX_Installer.ps1
```

### I want to uninstall ShieldX
**Windows:**
- Go to Settings → Apps → Installed apps
- Find "ShieldX Professional Antivirus"
- Click Uninstall

**Or PowerShell:**
```powershell
.\ShieldX_Installer.ps1 -Uninstall
```

### I need help with installation
See: **`QUICK_START_INSTALL.md`**

### I need detailed installation guide
See: **`SETUP_INSTALLER_GUIDE.md`**

### I need to build the application
See: **`MULTI_PLATFORM_BUILD_GUIDE.md`**

---

## ⚙️ Installation Architecture Choice

### 64-bit (x64) - Choose This For:
- ✅ Windows 10/11 (64-bit)
- ✅ Modern computers
- ✅ Better performance
- ✅ Most users

### 32-bit (x86) - Choose This For:
- ✅ Older Windows versions
- ✅ 32-bit Windows systems
- ✅ Legacy compatibility
- ✅ Special requirements

**Unsure?** Choose **64-bit** - it's recommended for most systems.

---

## 📋 What Happens When You Run ShieldX_Setup.bat

```
1. Checks Administrator permissions
   ↓
2. Verifies PowerShell is available
   ↓
3. Opens Architecture Selection Dialog
   ├─ 64-bit (x64)
   └─ 32-bit (x86)
   ↓
4. You select your version
   ↓
5. Installation begins
   ├─ Creates installation directory
   ├─ Copies application files
   ├─ Creates shortcuts
   ├─ Registers with Windows
   └─ Launches ShieldX
   ↓
6. ShieldX starts automatically ✅
```

---

## 🆘 Troubleshooting Quick Fixes

### "Access Denied" or "Permission" Error
→ Right-click `ShieldX_Setup.bat` and select "Run as administrator"

### Dialog doesn't appear
→ Only one version is built. Run: `build-all-platforms.bat`

### "Build files not found"
→ Build first: `build-all-platforms.bat`

### Can't find the shortcut after install
→ Check Desktop or Windows Start Menu under "ShieldX"

### Want to uninstall
→ Use Programs and Features in Windows Settings

---

## 📚 Documentation Map

```
QUICK_START_INSTALL.md
├─ Fast installation steps
├─ File descriptions
└─ Simple troubleshooting

SETUP_INSTALLER_GUIDE.md
├─ Installation methods (3 ways)
├─ Architecture selection guide
├─ Complete troubleshooting
├─ System requirements
├─ FAQ section
└─ Advanced options

GUI_SETUP_INSTALLER_SUMMARY.md
├─ Technical overview
├─ What's new
├─ Implementation details
└─ Before/After comparison

MULTI_PLATFORM_BUILD_GUIDE.md
├─ Build instructions
├─ Development setup
├─ Build commands
└─ Verification steps
```

---

## 🎓 For Developers

### Building for Both Platforms

```batch
REM Option 1: Automatic (Easy)
build-all-platforms.bat

REM Option 2: Manual
dotnet publish -c Release -p:Platform=x64
dotnet publish -c Release -p:Platform=x86
```

### Project Configuration
- File: **ShieldX.csproj**
- Supports: Both x86 and x64 platforms
- Conditional settings for each architecture

### Output Locations
- 64-bit: `bin\Release\net8.0-windows\win-x64\`
- 32-bit: `bin\Release\net8.0-windows\win-x86\`

---

## 🔍 File Structure

```
ShieldX_Antivirus/
├── ShieldX_Setup.bat                    ← START HERE
├── ShieldX_Installer.ps1               ← PowerShell installer
├── build-all-platforms.bat             ← Build helper
├── QUICK_START_INSTALL.md              ← Quick reference
├── SETUP_INSTALLER_GUIDE.md            ← Full guide
├── GUI_SETUP_INSTALLER_SUMMARY.md      ← Technical details
├── MULTI_PLATFORM_BUILD_GUIDE.md       ← Build guide
├── INSTALLER_FILES_INDEX.md            ← This file
└── bin/
    └── Release/
        └── net8.0-windows/
            ├── win-x64/                ← 64-bit build
            └── win-x86/                ← 32-bit build
```

---

## ✅ System Requirements

### For Installation
- Windows 7 SP1 or later
- Administrator privileges
- .NET 8.0 Runtime installed
- 100 MB free disk space

### For 64-bit Version
- 64-bit Windows (Vista or later)
- .NET 8.0 Runtime 64-bit
- 512 MB RAM minimum

### For 32-bit Version
- Windows XP or later (any bit)
- .NET 8.0 Runtime 32-bit
- 256 MB RAM minimum

---

## 📞 Need Help?

| Issue | See |
|-------|-----|
| Quick installation steps | QUICK_START_INSTALL.md |
| Installation problems | SETUP_INSTALLER_GUIDE.md |
| Understanding architecture choice | SETUP_INSTALLER_GUIDE.md |
| How to build applications | MULTI_PLATFORM_BUILD_GUIDE.md |
| Technical details | GUI_SETUP_INSTALLER_SUMMARY.md |

---

## Version Information

- **Application**: ShieldX Professional Antivirus
- **Version**: 3.1.0
- **.NET**: .NET 8.0
- **Architectures**: x86 (32-bit), x64 (64-bit)
- **Windows Support**: 7 SP1, 8, 8.1, 10, 11

---

**Last Updated:** April 24, 2026
**Status:** ✅ Multi-platform installer with GUI selection ready
