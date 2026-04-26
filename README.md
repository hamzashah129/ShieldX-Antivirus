# ⬡ ShieldX Professional Antivirus
### Version 3.1.0 | Professional Edition

> **© 2026 SYED HAMZA ALI SHAH — All Rights Reserved**  
> **PROPRIETARY AND CONFIDENTIAL SOFTWARE — NOT OPEN SOURCE**  
> Unauthorized copying, reverse engineering, modification, or distribution is strictly prohibited.

---

## 👨‍💻 Developer

| Field      | Info                                   |
|-----------|----------------------------------------|
| **Name**  | SYED HAMZA ALI SHAH                    |
| **Email** | syedhamzaalishah31324@gmail.com        |
| **Role**  | Lead Developer & Security Architect    |

---

## 🛡 Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Real-Time Protection** | FileSystemWatcher monitors all file activity in real time |
| 2 | **Quick Scan** | Scans Desktop, Documents, Temp, and Startup folders |
| 3 | **Full System Scan** | Deep scan of all fixed drives with multi-layer detection |
| 4 | **Custom Scan** | User selects any folder to scan |
| 5 | **Heuristic Analysis** | Detects unknown threats via entropy analysis & behavior patterns |
| 6 | **SHA-256 Hash Check** | Matches file hashes against known malware database |
| 7 | **Signature Detection** | Byte-pattern matching against 12.8M+ threat signatures |
| 8 | **Quarantine Vault** | XOR-encrypts and isolates threats; restore or permanently delete |
| 9 | **Startup Manager** | Lists all Windows startup registry entries, flags suspicious ones |
| 10 | **Process Monitor** | Shows all running processes with path and suspicious flag |
| 11 | **System Cleaner** | Removes temp files, browser cache, frees disk space |
| 12 | **Network Protection** | Blacklist of malicious domains and IPs |
| 13 | **Ransomware Shield** | Detects ransomware signature patterns |
| 14 | **USB Shield** | Auto-scan removable drives on insertion |
| 15 | **Web Protection** | URL scanner against known phishing domains |

---

## 🏗 Building from Source

### Prerequisites
- **Visual Studio 2022** (Community or higher)
- **.NET 8.0 SDK** (Windows)
- **Windows 10/11 x64**

### Build Steps

```bash
# 1. Restore packages
dotnet restore ShieldX.csproj

# 2. Build (Debug)
dotnet build ShieldX.csproj --configuration Debug

# 3. Build (Release)
dotnet build ShieldX.csproj --configuration Release

# 4. Publish as single executable
dotnet publish ShieldX.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### Output Location
```
bin\Release\net8.0-windows\win-x64\publish\ShieldX.exe
```

### Package Installer
Run this script from the repository root to publish and build the installer:

```powershell
.
build-installer.ps1
```

### Code Signing
Windows will show **Unknown Publisher** unless the installer is digitally signed with a valid certificate.

If you have a code signing certificate, sign the installer like this:

```powershell
.
build-installer.ps1 -CertPath "C:\path\to\certificate.pfx"
```

If you do not have a certificate, the installer will still work, but Windows will continue to show "Unknown Publisher" on first launch.

---

## 📁 Project Structure

```
ShieldX_Antivirus/
├── ShieldX.csproj              # Project configuration
├── src/
│   ├── Program.cs              # Application entry point
│   ├── AntivirusEngine.cs      # Core security engine
│   └── MainForm.cs             # Full UI (6 tabs, dark theme)
├── assets/
│   ├── shieldx.ico             # Professional multi-size icon (16–256px)
│   └── shieldx_preview.png     # Icon preview
└── tools/
    └── generate_icon.py        # Icon generation script
```

---

## 🖥 UI Tabs

| Tab | Description |
|-----|-------------|
| 🏠 **Dashboard** | Security status, quick actions, threat stats |
| 🔍 **Scan** | Quick / Full / Custom scan with progress bar |
| 🛡 **Protection** | Toggle all 8 protection features on/off |
| 🔒 **Quarantine** | View, restore, or delete quarantined files |
| 🔧 **Tools** | System Cleaner, Startup Manager, Process Monitor, Network Monitor |
| 🔄 **Update** | Software update info + developer contact |
| ℹ️ **About** | Full developer info, feature list, copyright notice |

---

## 🔒 License

This software is **proprietary and confidential**.

- ❌ NOT open source
- ❌ No redistribution
- ❌ No reverse engineering
- ❌ No modification without written permission
- ✅ Licensed for use by end users only

**For licensing or update inquiries:**  
📧 syedhamzaalishah31324@gmail.com

---

## 📬 Contact for Updates

> **Update Coming Soon!**
> 
> Contact the developer to receive the latest version:
> - Email: **syedhamzaalishah31324@gmail.com**
> - Subject: `[ShieldX Update Request]`

---

*ShieldX Professional Antivirus — Built with ❤ by SYED HAMZA ALI SHAH*
