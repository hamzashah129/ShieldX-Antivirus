# Quick Start: Create EXE Installer with 32-bit & 64-bit Options

## TL;DR (Quick Version)

### 1. Install NSIS
- Download: https://nsis.sourceforge.io/Download
- Run the installer
- Choose "Full Installation"

### 2. Build Applications
```batch
build-all-platforms.bat
```

### 3. Create EXE Installer
```batch
build-exe-installer.bat
```

### 4. Done!
Your EXE installer is ready: **`ShieldX_Professional_Antivirus_v3.1.0_Setup.exe`**

---

## What You Get

✅ Professional Windows installer EXE  
✅ Welcome screen  
✅ License agreement page  
✅ Architecture selection dialog (32-bit/64-bit)  
✅ Installation progress  
✅ Auto-creates shortcuts  
✅ Auto-adds to Programs & Features  

---

## Installation Architecture Choice

When users run the EXE, they see:

```
┌─────────────────────────────────┐
│ Select Installation Architecture│
│                                 │
│ ◉ 64-bit (x64) - Recommended   │
│   • Better performance          │
│   • For modern systems          │
│                                 │
│ ○ 32-bit (x86) - Legacy        │
│   • Legacy compatibility        │
│   • Works everywhere            │
│                                 │
│ [< Back] [Next >] [Cancel]      │
└─────────────────────────────────┘
```

Users simply:
1. Select their version
2. Click Next
3. Complete installation

---

## File Locations

| Step | File | Purpose |
|------|------|---------|
| 1. Build | `build-all-platforms.bat` | Build both architectures |
| 2. Compile | `build-exe-installer.bat` | Create the EXE |
| 3. Output | `ShieldX_Professional_Antivirus_v3.1.0_Setup.exe` | Final installer to distribute |

---

## Complete Step-by-Step

### Step 1: Install NSIS

1. Visit: https://nsis.sourceforge.io/Download
2. Download the latest version (3.x)
3. Run installer.exe
4. Click "Next" through the wizard
5. Choose **Full Installation**
6. Complete installation

**Verification:**
```
File should exist at:
C:\Program Files (x86)\NSIS\makensis.exe
```

### Step 2: Prepare Build Files

```batch
cd d:\ShieldX_Antivirus
build-all-platforms.bat
```

**Expected output:**
- ✅ `bin\Release\net8.0-windows\win-x64\ShieldX.exe`
- ✅ `bin\Release\net8.0-windows\win-x86\ShieldX.exe`

### Step 3: Create EXE Installer

```batch
build-exe-installer.bat
```

**The script will:**
1. Check for NSIS installation
2. Verify build files exist
3. Verify LICENSE.txt exists
4. Compile the NSIS script
5. Create the EXE

**Output:**
```
ShieldX_Professional_Antivirus_v3.1.0_Setup.exe
```

### Step 4: Test the Installer

1. Right-click the EXE
2. Select "Run as administrator"
3. Click "Next" through the wizard
4. When asked, select 32-bit or 64-bit
5. Let it install
6. Verify shortcut appears on Desktop

---

## Manual Compilation (Alternative)

If `build-exe-installer.bat` doesn't work:

### Using NSIS GUI Compiler
1. Click Start Menu → NSIS → NSIS Compiler
2. Click "Open Script"
3. Select: `ShieldX_Installer.nsi`
4. Click "Compile NSI Scripts"
5. Wait for completion

### Using PowerShell
```powershell
$NSIS = "C:\Program Files (x86)\NSIS\makensis.exe"
& $NSIS ShieldX_Installer.nsi
```

### Using Command Line
```batch
"C:\Program Files (x86)\NSIS\makensis.exe" ShieldX_Installer.nsi
```

---

## Troubleshooting

### "NSIS is not installed"
→ Download and install NSIS from https://nsis.sourceforge.io/

### "Build files not found"
→ Run: `build-all-platforms.bat` first

### "LICENSE.txt not found"
→ Create a LICENSE.txt file or let the script create a default one

### "EXE creation fails"
→ Check NSIS installation path:
   - Should be: `C:\Program Files (x86)\NSIS\`
   - Or: `C:\Program Files\NSIS\`

---

## EXE Installer Features

✅ **Welcome Page**
- Friendly introduction
- Version number displayed

✅ **License Agreement**
- Shows LICENSE.txt
- User must agree to continue

✅ **Architecture Selection**
- Choose 64-bit (recommended)
- Or choose 32-bit (legacy)
- Descriptions for each option

✅ **Installation Progress**
- Shows what's being copied
- Progress bar
- Estimated time remaining

✅ **Completion**
- Success message
- Architecture installed shown
- Option to launch app

✅ **Windows Integration**
- Desktop shortcut created
- Start Menu folder created
- Added to Programs & Features
- Uninstall support

---

## Distribution

Your EXE installer is ready to share:

```
ShieldX_Professional_Antivirus_v3.1.0_Setup.exe
```

**Share via:**
- Email (if < 25 MB, upload to cloud)
- Cloud storage (OneDrive, Google Drive)
- Website download
- USB drive
- Network share

**File size:** ~100-150 MB (includes both 32-bit and 64-bit)

---

## Files Generated

After running `build-exe-installer.bat`, you'll have:

```
ShieldX_Antivirus/
├── ShieldX_Professional_Antivirus_v3.1.0_Setup.exe  ← YOUR INSTALLER
├── ShieldX_Installer.nsi                           ← NSIS script
├── build-exe-installer.bat                         ← Builder helper
└── bin/Release/net8.0-windows/
    ├── win-x64/                                    ← 64-bit build
    └── win-x86/                                    ← 32-bit build
```

---

## What Users See

### 1. Download & Run
```
User downloads: ShieldX_Professional_Antivirus_v3.1.0_Setup.exe
User runs it (Right-click → Run as Administrator)
```

### 2. Welcome Page
```
"Welcome to ShieldX Professional Antivirus v3.1.0 Setup"
[Next >]
```

### 3. License Agreement
```
License text from LICENSE.txt
☑ I Agree
[Next >]
```

### 4. Architecture Selection
```
◉ 64-bit (x64) - Recommended for modern systems
○ 32-bit (x86) - Legacy system support

User selects → [Next >]
```

### 5. Installation Progress
```
[████████████████░░░] 75%
Copying files...
```

### 6. Finish
```
Installation completed successfully!
Installed Architecture: x64-bit ✓
[Finish]
```

### 7. Application Starts
- ShieldX launches automatically
- Desktop shortcut created
- Available in Start Menu

---

## Environment Setup

### Prerequisites
- Windows 7 SP1 or later
- NSIS 3.x installed
- Both architectures built (x86 & x64)
- LICENSE.txt file

### System Requirements for Users
- Windows 7 SP1 or later
- Administrator rights to install
- ~100-150 MB disk space
- .NET 8.0 Runtime installed

---

## Key Differences from PowerShell Installer

| Feature | PowerShell | EXE |
|---------|-----------|-----|
| Entry Point | batch file | double-click EXE |
| User Interface | Dialog | Professional wizard |
| Architecture Choice | Yes | Yes |
| License Display | No | Yes |
| Windows Integration | Basic | Full |
| Professional Appearance | Medium | High |
| File Size | Small | 100-150 MB |

---

## Command Reference

```batch
REM 1. Build both architectures
build-all-platforms.bat

REM 2. Create EXE installer
build-exe-installer.bat

REM 3. Result: Installer EXE is ready
ShieldX_Professional_Antivirus_v3.1.0_Setup.exe

REM To test
ShieldX_Professional_Antivirus_v3.1.0_Setup.exe
REM (right-click → Run as Administrator)
```

---

## Next Steps

1. ✅ Install NSIS
2. ✅ Run `build-all-platforms.bat`
3. ✅ Run `build-exe-installer.bat`
4. ✅ Test the EXE installer
5. ✅ Distribute the EXE

**That's it! You now have a professional installer with 32-bit & 64-bit options!** 🎉
