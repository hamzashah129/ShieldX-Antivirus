# Quick Start: Installing ShieldX

## Fastest Way to Install

1. **Find the installer file:**
   - `ShieldX_Setup.bat`

2. **Right-click and select "Run as administrator"**

3. **Choose your version:**
   - **64-bit (x64)** - Most systems (Windows 10/11)
   - **32-bit (x86)** - Legacy or special compatibility needs

4. **Click Install button**

✅ Done! ShieldX will launch automatically.

---

## Files in Your ShieldX Directory

| File | Purpose |
|------|---------|
| `ShieldX_Setup.bat` | **Start here!** Main setup installer |
| `ShieldX_Installer.ps1` | PowerShell installer (advanced) |
| `build-all-platforms.bat` | Build for both architectures |
| `SETUP_INSTALLER_GUIDE.md` | Detailed installation guide |
| `MULTI_PLATFORM_BUILD_GUIDE.md` | Build instructions |

---

## Installation Options

### Option 1: GUI Setup (Easy - Recommended)
```batch
ShieldX_Setup.bat
```
- Right-click → Run as administrator
- Select your version from the dialog
- Click Install

### Option 2: PowerShell (Advanced)
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\ShieldX_Installer.ps1
```

### Option 3: Specific Architecture (Manual)
```powershell
# Install 64-bit
.\ShieldX_Installer.ps1 -Architecture x64

# Install 32-bit
.\ShieldX_Installer.ps1 -Architecture x86
```

---

## Architecture Guide

### 64-bit (x64) - Recommended
- ✅ Modern Windows 10/11 systems
- ✅ Better performance
- ✅ Up to 2TB+ memory support
- ✅ Recommended for most users

### 32-bit (x86) - Legacy Support
- ✅ Works on 32-bit and 64-bit Windows
- ✅ Legacy system compatibility
- ✅ Lower memory usage
- ✅ Use only if needed

---

## Troubleshooting

**Dialog doesn't appear?**
- Only one build version is available
- Build both: `build-all-platforms.bat`

**Permission denied?**
- Right-click → Run as Administrator
- Accept UAC prompt

**Builds not found?**
- Run: `build-all-platforms.bat`
- Or: `dotnet publish -c Release -p:Platform=x64`

---

## After Installation

1. ShieldX launches automatically
2. Check your Desktop for the shortcut
3. Access from Start Menu: ShieldX Folder
4. Uninstall via Programs and Features (if needed)

---

## Need More Help?

📖 See **SETUP_INSTALLER_GUIDE.md** for complete documentation
