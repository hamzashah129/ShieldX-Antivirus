# ShieldX Professional Antivirus v3.1.0 - Installation Guide

Welcome! ShieldX Professional Antivirus is ready to install. There are **two installation methods** available:

## **Option 1: Simple Installation (Recommended)**
**File**: `Install_ShieldX.bat`

**For users who want quick, no-fuss installation:**
1. Double-click `Install_ShieldX.bat`
2. Click "Yes" when prompted for admin permissions
3. Installation completes automatically
4. ShieldX launches and is ready to use

**Advantages:**
- ✅ No additional software required
- ✅ Fast and simple
- ✅ Automatic uninstall support
- ✅ Works on Windows 10/11

---

## **Option 2: Professional Installer (Inno Setup)**
**File**: `setup.iss`

**For users who prefer a traditional Windows installer:**
1. Install [Inno Setup](https://jrsoftware.org/isdl.php) (free)
2. Open `setup.iss` with Inno Setup
3. Click "Build" → "Compile"
4. Run the generated `ShieldX_v3.1.0_Setup.exe`

**Advantages:**
- ✅ Professional Windows installer interface
- ✅ Traditional Add/Remove Programs integration
- ✅ Custom installation options (shortcuts, context menu)
- ✅ Enterprise-ready

---

## System Requirements

- **OS**: Windows 10 or Windows 11 (64-bit)
- **RAM**: 2GB minimum, 4GB+ recommended
- **Disk Space**: 500MB for full installation
- **Admin Rights**: Required for installation and real-time protection

---

## Installation Paths

- **Installed to**: `C:\Program Files\ShieldX Professional Antivirus`
- **Shortcuts**: Desktop and Start Menu
- **Database**: `%AppData%\ShieldX\`
- **Quarantine**: `%AppData%\ShieldX\Quarantine\`

---

## Post-Installation

After installation:
1. **Real-time Protection** activates automatically
2. **Initial Database Update** - App will download threat definitions (~50MB)
3. **First Scan** - Recommended to run a quick scan from the Dashboard

---

## Uninstallation

### Using PowerShell Installer:
```powershell
powershell -ExecutionPolicy Bypass -File "installer\ShieldX_Installer.ps1" -Uninstall
```

### Using Inno Setup:
- Go to **Settings** → **Apps** → **Apps and Features**
- Find "ShieldX Professional Antivirus"
- Click **Uninstall**

---

## First Run Features

✅ **Auto-Update System** - Updates check automatically
✅ **Real-time Protection** - Background threat monitoring
✅ **Quarantine System** - Isolated threat storage
✅ **Encrypted Vault** - Password manager with AES-256
✅ **USB Auto-Scan** - Automatically scans USB drives
✅ **Update Badge** - Updates tab shows when new version available

---

## Support

- **GitHub**: https://github.com/hamzashah129/ShieldX-Antivirus
- **Issues**: https://github.com/hamzashah129/ShieldX-Antivirus/issues
- **Releases**: https://github.com/hamzashah129/ShieldX-Antivirus/releases

---

**Thank you for using ShieldX Professional Antivirus!**

For more information, visit the Updates tab in the application.
