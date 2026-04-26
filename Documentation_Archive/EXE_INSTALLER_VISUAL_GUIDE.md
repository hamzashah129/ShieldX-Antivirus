# ShieldX EXE Installer Visual Guide

## Installation Flow

```
User double-clicks:
ShieldX_Professional_Antivirus_v3.1.1_Setup.exe
                    ↓
            Admin Check
                    ↓
         ╔═════════════════════════════════════╗
         ║         WELCOME PAGE               ║
         ║                                     ║
         ║ Welcome to Setup                    ║
         ║                                     ║
         ║ Welcome to the ShieldX             ║
         ║ Professional Antivirus v3.1.1      ║
         ║ Setup wizard.                       ║
         ║                                     ║
         ║ This wizard will guide you through  ║
         ║ the installation process.           ║
         ║                                     ║
         ║ [Next >]  [Cancel]                 ║
         ╚═════════════════════════════════════╝
                    ↓
         ╔═════════════════════════════════════╗
         ║      LICENSE AGREEMENT PAGE        ║
         ║                                     ║
         ║ License Agreement                   ║
         ║                                     ║
         ║ Please read the following license:  ║
         ║                                     ║
         ║ ┌─────────────────────────────────┐ ║
         ║ │ ShieldX Professional Antivirus  │ ║
         ║ │                                  │ ║
         ║ │ Copyright (c) 2026...            │ ║
         ║ │                                  │ ║
         ║ │ License terms and conditions...  │ ║
         ║ │ [more text scrollable...]        │ ║
         ║ └─────────────────────────────────┘ ║
         ║                                     ║
         ║ ○ I do not agree                    ║
         ║ ◉ I agree to the license           ║
         ║                                     ║
         ║ [< Back] [Next >] [Cancel]         ║
         ╚═════════════════════════════════════╝
                    ↓
         ╔═════════════════════════════════════╗
         ║  ARCHITECTURE SELECTION PAGE       ║
         ║                                     ║
         ║ Select Installation Architecture    ║
         ║                                     ║
         ║ Choose which version to install    ║
         ║ on your system:                    ║
         ║                                     ║
         ║ ◉ 64-bit (x64)                     ║
         ║   Recommended for modern systems    ║
         ║   • Better performance on modern    ║
         ║     processors                     ║
         ║   • Supports systems with more     ║
         ║     than 3GB RAM                   ║
         ║   • Recommended for Windows        ║
         ║     10/11 64-bit systems           ║
         ║                                     ║
         ║ ○ 32-bit (x86)                     ║
         ║   Legacy system support            ║
         ║   • Compatible with legacy         ║
         ║     systems                        ║
         ║   • Lower memory footprint         ║
         ║   • Works on both 32-bit and      ║
         ║     64-bit Windows                ║
         ║                                     ║
         ║ [< Back] [Next >] [Cancel]         ║
         ╚═════════════════════════════════════╝
                    ↓
         ╔═════════════════════════════════════╗
         ║  COMPONENTS SELECTION PAGE         ║
         ║                                     ║
         ║ Select Components                   ║
         ║                                     ║
         ║ Select the components you want     ║
         ║ to install:                        ║
         ║                                     ║
         ║ ☑ ShieldX Professional Antivirus   ║
         ║   (Required)                        ║
         ║                                     ║
         ║ Install location:                   ║
         ║ C:\Program Files\ShieldX...         ║
         ║ [Browse...]                         ║
         ║                                     ║
         ║ Space required: 150 MB              ║
         ║ Space available: 500 GB             ║
         ║                                     ║
         ║ [< Back] [Next >] [Cancel]         ║
         ╚═════════════════════════════════════╝
                    ↓
         ╔═════════════════════════════════════╗
         ║  INSTALLATION PROGRESS PAGE        ║
         ║                                     ║
         ║ Installing ShieldX Professional    ║
         ║                                     ║
         ║ Status:                             ║
         ║ [████████████████░░░░░░░░] 60%     ║
         ║                                     ║
         ║ Currently copying:                 ║
         ║ ShieldX.exe                         ║
         ║                                     ║
         ║ Copying 98 MB of 163 MB            ║
         ║                                     ║
         ║ Installation log:                   ║
         ║ [1/4] Creating installation dir... ║
         ║ [2/4] Copying application files... ║
         ║ [3/4] Creating shortcuts...         ║
         ║ [4/4] Registering with Windows...  ║
         ║                                     ║
         ║ [Cancel]                            ║
         ╚═════════════════════════════════════╝
                    ↓
         ╔═════════════════════════════════════╗
         ║   INSTALLATION COMPLETE PAGE       ║
         ║                                     ║
         ║ Installation Complete              ║
         ║                                     ║
         ║ ✓ ShieldX Professional Antivirus   ║
         ║   v3.1.0 has been installed        ║
         ║   successfully!                    ║
         ║                                     ║
         ║ ✓ Architecture: x64-bit (64-bit)   ║
         ║                                     ║
         ║ Installed location:                ║
         ║ C:\Program Files\ShieldX...         ║
         ║                                     ║
         ║ What's next?                       ║
         ║ ☑ Launch ShieldX now              ║
         ║                                     ║
         ║ [< Back] [Finish]                  ║
         ╚═════════════════════════════════════╝
                    ↓
         ShieldX launches automatically ✓
```

---

## Installation Results

### Desktop
```
Desktop/
├── ShieldX [shortcut with icon]
└── (other items)
```

### Start Menu
```
Start Menu/
├── Programs/
│   ├── ShieldX/
│   │   ├── ShieldX Professional Antivirus [shortcut]
│   │   └── Uninstall ShieldX [shortcut]
│   └── (other programs)
└── (other items)
```

### Programs and Features
```
Settings → Apps → Installed apps

Name:                    ShieldX Professional Antivirus
Version:                 3.1.1
Publisher:               SYED HAMZA ALI SHAH
Install date:            April 24, 2026
Size:                    163 MB
Architecture:            x64-bit

[Uninstall]
```

### Installation Directory
```
C:\Program Files\ShieldX Professional Antivirus\
├── ShieldX.exe                 [Main application]
├── ShieldX.dll                 [Runtime files]
├── assets/
│   ├── shieldx.ico
│   └── (other assets)
├── appsettings.json
├── Uninstall.exe              [Uninstaller]
└── (other application files)
```

---

## Architecture Selection Details

### When 64-bit is Selected
```
Selected: x64-bit (64-bit)

Files copied from:
bin\Release\net8.0-windows\win-x64\*

Installation details:
✓ 64-bit version installed
✓ .NET 8.0 Runtime x64 required
✓ Optimized for 64-bit Windows
✓ Supports >3GB RAM
✓ Better performance

Registry entry:
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX
├── InstalledArchitecture = x64
└── DisplayName = ShieldX Professional Antivirus 3.1.1
```

### When 32-bit is Selected
```
Selected: x86-bit (32-bit)

Files copied from:
bin\Release\net8.0-windows\win-x86\*

Installation details:
✓ 32-bit version installed
✓ .NET 8.0 Runtime x86 required
✓ Works on 32-bit and 64-bit Windows
✓ Lower memory footprint
✓ Legacy compatibility

Registry entry:
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX
├── InstalledArchitecture = x86
└── DisplayName = ShieldX Professional Antivirus 3.1.1
```

---

## Installation Steps Breakdown

### Step 1: Admin Check
```
Running EXE...
├─ Check for Administrator privileges
├─ Show UAC prompt if needed
└─ Continue if approved
```

### Step 2: Display Welcome Page
```
┌─ Window title: ShieldX Setup Wizard
├─ Show application name and version
├─ Brief introduction text
└─ [Next >] button to continue
```

### Step 3: License Agreement
```
┌─ Display LICENSE.txt content
├─ Read-only text box
├─ User must check "I Agree"
└─ [Next >] enabled only after agreement
```

### Step 4: Architecture Selection
```
┌─ Show radio buttons for:
│  ├─ 64-bit (x64) - PRESELECTED
│  └─ 32-bit (x86)
├─ Show descriptions for each
├─ User clicks [Next >]
└─ Selection stored in $SelectedArchitecture
```

### Step 5: Components Selection
```
┌─ Show components list
├─ ShieldX Professional Antivirus (checked)
├─ Show installation location
├─ Show required space (150 MB)
├─ Show available space
└─ User confirms [Next >]
```

### Step 6: Installation
```
┌─ Create C:\Program Files\ShieldX...
├─ Copy files from selected architecture
│  ├─ If x64: copy from win-x64 folder
│  └─ If x86: copy from win-x86 folder
├─ Create Desktop shortcut
├─ Create Start Menu folder
├─ Update Windows Registry
└─ Show progress bar
```

### Step 7: Finish
```
┌─ Show success message
├─ Display installed architecture
├─ Option to launch ShieldX
└─ Close installer
```

---

## User Experience Flow

### User's Perspective

1. **Downloads EXE**
   ```
   Downloaded: ShieldX_Professional_Antivirus_v3.1.1_Setup.exe
   File size: ~120 MB
   ```

2. **Runs Setup**
   ```
   Right-click EXE → Run as Administrator
   → UAC prompt appears → Click [Yes]
   ```

3. **Sees Welcome**
   ```
   Professional wizard window
   Company branding
   Version number displayed
   ```

4. **Reads License**
   ```
   License text displayed
   Must agree to continue
   ```

5. **Selects Architecture**
   ```
   Two clear options: 64-bit or 32-bit
   Descriptions help decision
   Can easily switch selection
   ```

6. **Confirms Installation**
   ```
   Shows what will be installed
   Shows where it will be installed
   Shows how much space needed
   ```

7. **Watches Installation**
   ```
   Progress bar shows status
   Details of files being copied
   Takes 2-5 minutes typically
   ```

8. **Completes**
   ```
   Success message
   Architecture confirmed
   Desktop shortcut ready
   App launches automatically
   ```

---

## File Architecture

### NSIS Script Components

```
ShieldX_Installer.nsi
├── Configuration
│   ├── Product name, version, publisher
│   ├── Output filename
│   ├── Installation directory
│   └── Version information
│
├── UI Pages
│   ├── Welcome page
│   ├── License page (LICENSE.txt)
│   ├── Custom Architecture page
│   ├── Components page
│   ├── Installation progress
│   └── Finish page
│
├── Installation Section
│   ├── Create directories
│   ├── Copy files (based on selected architecture)
│   ├── Create shortcuts
│   ├── Update registry
│   └── Create uninstaller
│
├── Architecture Selection
│   ├── Radio buttons for x64/x86
│   ├── Descriptions
│   ├── Default selection logic
│   └── User choice storage
│
└── Uninstaller
    ├── Remove shortcuts
    ├── Remove files
    ├── Remove registry entries
    └── Remove uninstaller
```

---

## Post-Installation

### What User Gets

```
✓ ShieldX installed to:
  C:\Program Files\ShieldX Professional Antivirus\

✓ Shortcuts created:
  • Desktop: ShieldX shortcut
  • Start Menu: ShieldX folder

✓ Registry updated:
  • Programs and Features entry
  • Uninstall information

✓ Can be uninstalled via:
  • Programs and Features (GUI)
  • Uninstall shortcut in Start Menu
  • Uninstall.exe in installation folder

✓ Architecture info saved:
  • Stored in registry
  • Shown in Properties
  • Used for future updates
```

---

## Size Reference

| Component | Size |
|-----------|------|
| 64-bit build | ~80-90 MB |
| 32-bit build | ~75-85 MB |
| NSIS wrapper | ~10-20 MB |
| **Total EXE** | **~100-150 MB** |

---

## Visual Mockup Example

### Main Installation Window
```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ ShieldX Professional Antivirus v3.1.0 Setup ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                               ┃
┃   [Window content changes per page]          ┃
┃                                               ┃
┃   Page 1: Welcome                            ┃
┃   Page 2: License Agreement                  ┃
┃   Page 3: Architecture Selection       ← NEW ┃
┃   Page 4: Components                         ┃
┃   Page 5: Installation Progress              ┃
┃   Page 6: Completion                         ┃
┃                                               ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃ [< Back]  [Next >]  [Cancel]                 ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

---

## Summary

Your EXE installer provides:

✅ **Professional UI** - Windows Forms-style wizard  
✅ **Clear Architecture Choice** - 32-bit or 64-bit selection  
✅ **License Agreement** - Displays LICENSE.txt  
✅ **Installation Progress** - Shows what's happening  
✅ **Windows Integration** - Shortcuts, registry, Programs & Features  
✅ **Easy Distribution** - Single EXE file (~120 MB)  
✅ **User Friendly** - Wizard guides through each step  

**Users just need to:**
1. Download the EXE
2. Right-click → Run as Administrator
3. Click through the wizard
4. Select their architecture
5. Let it install!
