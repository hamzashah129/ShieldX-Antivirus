# Professional Visual Studio Installer-Style Setup for ShieldX

## Overview
This package implements a professional Windows installer for ShieldX Professional Antivirus that matches the Visual Studio Installer appearance and functionality, along with an integrated Installer Manager window inside the application.

## Files Created

### 1. **installer/ShieldX_Setup.iss** - Inno Setup 6 Configuration
**Purpose**: Professional installer script with VS Installer-style appearance

**Key Features**:
- Modern WizardStyle with custom dark theme
- Installation types: Full, Compact, Custom
- Components: Core Engine, Real-Time Protection, Password Vault, Network Monitor, Context Menu Integration
- Custom installation tasks: Desktop shortcut, Taskbar pin, Auto-startup, Context menu, System tray
- Registry entries for Windows integration
- Context menu extension (right-click scan with ShieldX)
- Automatic app launch on first install
- Silent installer support with /MODIFY, /REPAIR, /UNINSTALL parameters

**Usage**:
```bash
# Compile the installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer/ShieldX_Setup.iss

# Output: installer/Output/ShieldX_Professional_v3.1.0_Setup_*.exe
```

**Registry Keys Created**:
- `HKLM\Software\ShieldX\Professional` - Installation metadata
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` - Startup entry
- `HKCR\*\shell\ScanWithShieldX` - Context menu (files)
- `HKCR\Directory\shell\ScanWithShieldX` - Context menu (folders)
- `HKCR\Drive\shell\ScanWithShieldX` - Context menu (drives)

---

### 2. **src/Views/InstallerManagerWindow.xaml** - Professional UI
**Purpose**: Visual Studio Installer-style management window for ShieldX installation

**Design Features**:
- Gradient header with app branding
- Tab navigation: "Installed" and "Available"
- Installed section shows:
  - Large app card with icon and version info
  - Action buttons: Launch, Update, Modify, More (dropdown)
  - Installed components grid (Protection, Security Tools, Monitoring)
  - Installation details (version, location, disk usage, status)
  - More menu with: Repair, Open Folder, Copy Path, Uninstall
- Available updates section with check updates button
- Modern dark theme matching ShieldX branding
- Bottom footer with links and close button

**Responsive Elements**:
- Launch button (Primary, cyan gradient)
- Update button (Check for new versions)
- Modify button (Add/remove components)
- Repair button (Fix corrupted files)
- Uninstall button (Remove application)

---

### 3. **src/Views/InstallerManagerWindow.xaml.cs** - Code-Behind
**Purpose**: Logic for installer manager window operations

**Methods**:
- `LoadInstallInfo()` - Reads registry, calculates disk usage
- `TabInstalled_Click()` - Switch to installed view
- `TabAvailable_Click()` - Switch to updates view
- `Launch_Click()` - Launch ShieldX application
- `Update_Click()` - Check for updates
- `Modify_Click()` - Launch installer in modify mode
- `Repair_Click()` - Launch installer in repair mode
- `OpenFolder_Click()` - Open installation folder
- `CopyPath_Click()` - Copy install path to clipboard
- `Uninstall_Click()` - Launch uninstaller
- `LaunchInstaller()` - Find and launch installer with admin rights

**Admin Elevation**: Uses `verb = "runas"` for administrator requests

---

### 4. **src/Views/AboutView.xaml** - Updated with Installer Manager Button
**Addition**: New button section for installer management

```xaml
<!-- ══ MANAGE INSTALLATION BUTTON ══ -->
<Button Content="⚙ Manage Installation"
        Click="ManageInstallation_Click"
        Background="#252D3D"
        Foreground="White"
        Padding="16,12" />
```

**Location**: Added after copyright footer, accessible from About view

---

### 5. **src/Views/AboutView.xaml.cs** - Updated Code-Behind
**Addition**: Event handler to open installer manager

```csharp
private void ManageInstallation_Click(object sender, RoutedEventArgs e)
{
    var installerManager = new InstallerManagerWindow();
    installerManager.ShowDialog();
}
```

---

### 6. **build.ps1** - Professional Build Script
**Purpose**: Automated build and installer creation

**Features**:
- Environment verification (.NET SDK check)
- Clean build (removes bin/obj directories)
- NuGet package restoration
- Release build for x64 architecture
- Inno Setup compilation (if installed)
- Build summary with file sizes
- Optional build archiving
- Colored console output with progress indicators

**Usage**:
```powershell
# Full build (publish + installer)
.\build.ps1

# Skip installer compilation
.\build.ps1 -SkipInstaller

# Skip publishing (reuse existing build)
.\build.ps1 -SkipPublish
```

**Output**:
- Published app: `bin/Release/net8.0-windows/win-x64/publish/`
- Installer: `installer/Output/ShieldX_Professional_v3.1.0_Setup_*.exe`

---

## Installer Parameter Support

The Inno Setup installer supports command-line parameters for silent/repair/modify:

```batch
ShieldX_Professional_v3.1.0_Setup.exe /MODIFY      # Add/remove components
ShieldX_Professional_v3.1.0_Setup.exe /REPAIR      # Repair installation
ShieldX_Professional_v3.1.0_Setup.exe /UNINSTALL   # Uninstall silently
ShieldX_Professional_v3.1.0_Setup.exe /SILENT      # Silent install
ShieldX_Professional_v3.1.0_Setup.exe /VERYSILENT  # Very silent
```

---

## Installation Features

### What Gets Installed
1. **Core Application**
   - ShieldX.exe and all dependencies
   - .NET runtime files (if needed)
   - Configuration files

2. **Directories Created**
   - `%ProgramFiles%\ShieldX Professional Antivirus\`
   - `%LOCALAPPDATA%\ShieldX\Quarantine\`
   - `%LOCALAPPDATA%\ShieldX\Logs\`
   - `%LOCALAPPDATA%\ShieldX\Vault\`

3. **Registry Entries**
   - Software registration
   - Uninstall information
   - Context menu entries
   - Startup entry (if task selected)

4. **Context Menu Integration**
   - "Scan with ShieldX" on files
   - "Scan with ShieldX" on folders
   - "Scan with ShieldX" on drives

5. **Shortcuts Created**
   - Start Menu entry
   - Desktop shortcut (if selected)
   - Taskbar pin (if selected)

---

## Accessing Installer Manager

### From Inside ShieldX
1. Open ShieldX application
2. Navigate to About section
3. Click "⚙ Manage Installation" button
4. Installer Manager window opens

### Features in Installer Manager
- **Launch**: Opens running instance of ShieldX
- **Update**: Checks for newer versions
- **Modify**: Add/remove optional components
- **Repair**: Fix corrupted installation files
- **Uninstall**: Remove ShieldX from system
- **Open Folder**: Browse installation directory
- **Copy Path**: Copy install path to clipboard
- **Status Display**: Shows version, location, disk usage, runtime status

---

## System Requirements

### Build Requirements
- Windows 10/11 x64
- .NET 8 SDK
- Visual Studio 2022 or Rider IDE (optional, for editing)
- Inno Setup 6 (https://jrsoftware.org/isdl.php)

### Runtime Requirements
- Windows 10/11 x64
- .NET 8 Runtime (included in installer)
- Administrator privileges for installation

---

## Build Process

### Step 1: Prepare Build Environment
```powershell
cd C:\Path\To\ShieldX_Antivirus
$env:PATH += ";C:\Program Files (x86)\Inno Setup 6"
```

### Step 2: Execute Build Script
```powershell
.\build.ps1
```

### Step 3: Review Build Output
- Check `bin/Release/net8.0-windows/win-x64/publish/` for binaries
- Check `installer/Output/` for installer executable
- Review console output for any warnings

### Step 4: Test Installer
```powershell
# Test install
.\installer\Output\ShieldX_Professional_v3.1.0_Setup_*.exe

# Test modify/repair
ShieldX_Professional_v3.1.0_Setup.exe /MODIFY
ShieldX_Professional_v3.1.0_Setup.exe /REPAIR

# Test uninstall
# Use Control Panel > Programs > Programs and Features
# OR search for "ShieldX" in Control Panel and click uninstall
```

---

## Customization

### Modify Installer Appearance
Edit `installer/ShieldX_Setup.iss`:
- `WizardStyle=modern` - Installer wizard style
- Colors in [Code] section `InitializeWizard()` procedure
- Custom messages in [CustomMessages] section

### Modify Installer Manager Appearance
Edit `src/Views/InstallerManagerWindow.xaml`:
- Colors: Update RGB values in `LinearGradientBrush` definitions
- Buttons: Modify in Window.Resources Style definitions
- Layout: Adjust Grid/StackPanel dimensions

### Add Custom Installation Tasks
Edit `installer/ShieldX_Setup.iss` [Tasks] section:
```ini
Name: "custom_task"; Description: "My Custom Task"; GroupDescription: "Optional:"; Flags: unchecked
```

---

## Troubleshooting

### Installer Won't Start
1. Verify Inno Setup 6 is installed
2. Check `installer/ShieldX_Setup.iss` syntax
3. Run build script with `-SkipInstaller` to test app build first

### Installer Manager Window Won't Open
1. Verify registry entry: `HKLM\Software\ShieldX\Professional\InstallPath`
2. Check if ShieldX.exe exists in install directory
3. Review application logs in `%LOCALAPPDATA%\ShieldX\`

### Context Menu Integration Fails
1. Check registry: `HKCR\*\shell\ScanWithShieldX`
2. Verify ShieldX.exe supports `--scan` parameter
3. Test manually: `ShieldX.exe --scan "C:\path\to\file"`

### Uninstall Fails
1. Close all ShieldX instances
2. Run installer in REPAIR mode first
3. Delete `%LOCALAPPDATA%\ShieldX\` manually if needed

---

## Distribution

### Create Release Package
```powershell
# Build final release
.\build.ps1

# Locate installer
$installer = Get-ChildItem "installer/Output/ShieldX_*.exe" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

# Upload to release server
# GitHub Releases: https://github.com/SyedHamzaAliShah/ShieldX/releases
```

### Sign Installer (Optional)
See `temp-sign.ps1` for code signing instructions

---

## Version Information

**ShieldX Professional v3.1.0**
- Build Date: April 13, 2026
- Framework: .NET 8 WPF
- Installer: Inno Setup 6
- Installer Version: 1.0.0

---

## Support

For issues or questions:
- Email: syedhamzaalishah31324@gmail.com
- GitHub: https://github.com/SyedHamzaAliShah/ShieldX
- Issues: https://github.com/SyedHamzaAliShah/ShieldX/issues

---

## License

ShieldX Professional Antivirus
Copyright © 2026 SYED HAMZA ALI SHAH
All Rights Reserved
