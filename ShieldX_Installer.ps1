# ShieldX Professional Antivirus v3.1.0 Installer
# This script installs ShieldX to Program Files and creates shortcuts
# Supports both 32-bit (x86) and 64-bit (x64) systems with user selection

param(
    [switch]$Uninstall,
    [string]$Architecture = "" # Optional: x86, x64, or auto-detect
)

# Constants
$AppName = "ShieldX.exe"
$ProjectRoot = "C:\Users\SYED HAMZA ALI SHAH\Downloads\ShieldX_Antivirus"
$InstallPath = "C:\Program Files\ShieldX Professional Antivirus"
$DesktopShortcut = "$env:USERPROFILE\Desktop\ShieldX.lnk"
$StartMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ShieldX\ShieldX.lnk"
$StartMenuFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ShieldX"

# Admin elevation check
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "This installer requires administrator privileges. Please run as Administrator."
    exit 1
}

# Function to show architecture selection dialog
function Show-ArchitectureSelectionDialog {
    param(
        [string]$x86BuildPath,
        [string]$x64BuildPath,
        [bool]$x86Available,
        [bool]$x64Available,
        [string]$DetectedArch
    )

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "ShieldX Professional Antivirus v3.1.0 - Architecture Selection"
    $form.Size = New-Object System.Drawing.Size(600, 350)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = "FixedDialog"
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.TopMost = $true

    # Title Label
    $labelTitle = New-Object System.Windows.Forms.Label
    $labelTitle.Text = "Select Installation Version"
    $labelTitle.Font = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
    $labelTitle.Location = New-Object System.Drawing.Point(20, 20)
    $labelTitle.Size = New-Object System.Drawing.Size(540, 35)
    $form.Controls.Add($labelTitle)

    # Info Label
    $labelInfo = New-Object System.Windows.Forms.Label
    $labelInfo.Text = "Choose which version to install on your system:"
    $labelInfo.Font = New-Object System.Drawing.Font("Segoe UI", 10)
    $labelInfo.Location = New-Object System.Drawing.Point(20, 60)
    $labelInfo.Size = New-Object System.Drawing.Size(540, 25)
    $form.Controls.Add($labelInfo)

    # Radio buttons container
    $groupBox = New-Object System.Windows.Forms.GroupBox
    $groupBox.Text = "Available Versions"
    $groupBox.Location = New-Object System.Drawing.Point(20, 95)
    $groupBox.Size = New-Object System.Drawing.Size(540, 180)
    $groupBox.Font = New-Object System.Drawing.Font("Segoe UI", 10)

    # x64 Radio Button
    $radioX64 = New-Object System.Windows.Forms.RadioButton
    $radioX64.Text = "64-bit (x64) - $( if ($x64Available) { 'Recommended for modern systems' } else { 'Not Available' } )"
    $radioX64.Location = New-Object System.Drawing.Point(20, 30)
    $radioX64.Size = New-Object System.Drawing.Size(500, 25)
    $radioX64.Enabled = $x64Available
    if ($DetectedArch -eq "x64" -and $x64Available) { $radioX64.Checked = $true }
    $groupBox.Controls.Add($radioX64)

    # x64 Description
    $labelX64Desc = New-Object System.Windows.Forms.Label
    $labelX64Desc.Text = "  • Supports systems with more than 3GB RAM`n  • Modern 64-bit processors`n  • Better performance on 64-bit Windows"
    $labelX64Desc.Location = New-Object System.Drawing.Point(40, 55)
    $labelX64Desc.Size = New-Object System.Drawing.Size(480, 60)
    $labelX64Desc.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    $labelX64Desc.ForeColor = [System.Drawing.Color]::Gray
    $groupBox.Controls.Add($labelX64Desc)

    # x86 Radio Button
    $radioX86 = New-Object System.Windows.Forms.RadioButton
    $radioX86.Text = "32-bit (x86) - $( if ($x86Available) { 'Legacy system support' } else { 'Not Available' } )"
    $radioX86.Location = New-Object System.Drawing.Point(20, 120)
    $radioX86.Size = New-Object System.Drawing.Size(500, 25)
    $radioX86.Enabled = $x86Available
    if ($DetectedArch -eq "x86" -and $x86Available) { $radioX86.Checked = $true }
    $groupBox.Controls.Add($radioX86)

    # x86 Description
    $labelX86Desc = New-Object System.Windows.Forms.Label
    $labelX86Desc.Text = "  • Compatible with legacy systems`n  • Lower memory footprint`n  • Works on both 32-bit and 64-bit Windows"
    $labelX86Desc.Location = New-Object System.Drawing.Point(40, 145)
    $labelX86Desc.Size = New-Object System.Drawing.Size(480, 60)
    $labelX86Desc.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    $labelX86Desc.ForeColor = [System.Drawing.Color]::Gray
    $groupBox.Controls.Add($labelX86Desc)

    $form.Controls.Add($groupBox)

    # Install Button
    $buttonInstall = New-Object System.Windows.Forms.Button
    $buttonInstall.Text = "Install"
    $buttonInstall.Location = New-Object System.Drawing.Point(400, 290)
    $buttonInstall.Size = New-Object System.Drawing.Size(150, 35)
    $buttonInstall.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $buttonInstall.BackColor = [System.Drawing.Color]::FromArgb(0, 120, 215)
    $buttonInstall.ForeColor = [System.Drawing.Color]::White
    $buttonInstall.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
    $form.Controls.Add($buttonInstall)

    # Cancel Button
    $buttonCancel = New-Object System.Windows.Forms.Button
    $buttonCancel.Text = "Cancel"
    $buttonCancel.Location = New-Object System.Drawing.Point(250, 290)
    $buttonCancel.Size = New-Object System.Drawing.Size(140, 35)
    $buttonCancel.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $form.Controls.Add($buttonCancel)

    $result = $form.ShowDialog()

    if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
        if ($radioX64.Checked) {
            return "x64"
        } elseif ($radioX86.Checked) {
            return "x86"
        }
    }
    return $null
}

# Function to check and select architecture
function Select-Architecture {
    $x86Build = "win-x86"
    $x64Build = "win-x64"
    
    $x86BuildPath = "$ProjectRoot\bin\Release\net8.0-windows\$x86Build"
    $x64BuildPath = "$ProjectRoot\bin\Release\net8.0-windows\$x64Build"
    
    $x86Available = Test-Path "$x86BuildPath\$AppName"
    $x64Available = Test-Path "$x64BuildPath\$AppName"

    if (-not $x86Available -and -not $x64Available) {
        Write-Host "ERROR: No builds found!" -ForegroundColor Red
        Write-Host "Please build ShieldX for at least one architecture:" -ForegroundColor Yellow
        Write-Host "  dotnet publish -c Release -p:Platform=x86" -ForegroundColor Gray
        Write-Host "  dotnet publish -c Release -p:Platform=x64" -ForegroundColor Gray
        exit 1
    }

    # If only one build is available, use it
    if ($x86Available -and -not $x64Available) {
        Write-Host "Only 32-bit version found. Using x86..." -ForegroundColor Yellow
        return "x86"
    }
    if ($x64Available -and -not $x86Available) {
        Write-Host "Only 64-bit version found. Using x64..." -ForegroundColor Yellow
        return "x64"
    }

    # Both available - show selection dialog
    $DetectedArch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    $SelectedArch = Show-ArchitectureSelectionDialog -x86BuildPath $x86BuildPath -x64BuildPath $x64BuildPath -x86Available $x86Available -x64Available $x64Available -DetectedArch $DetectedArch

    if ($null -eq $SelectedArch) {
        Write-Host "Installation cancelled by user." -ForegroundColor Yellow
        exit 0
    }

    return $SelectedArch
}

function Install-ShieldX {
    # Select architecture (shows dialog if both available)
    $SelectedArchitecture = Select-Architecture
    
    $RuntimeID = if ($SelectedArchitecture -eq "x64") { "win-x64" } else { "win-x86" }
    $BuildPath = "$ProjectRoot\bin\Release\net8.0-windows\$RuntimeID"

    Write-Host ""
    Write-Host "ShieldX Professional Antivirus v3.1.0 Installer" -ForegroundColor Cyan
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "Installing: $SelectedArchitecture-bit version ($RuntimeID)" -ForegroundColor Green
    Write-Host ""

    # Check if build exists for the selected architecture
    if (-not (Test-Path "$BuildPath\$AppName")) {
        Write-Host "ERROR: Build files not found for $SelectedArchitecture-bit at:" -ForegroundColor Red
        Write-Host "  $BuildPath" -ForegroundColor Red
        Write-Host "Please build ShieldX first:" -ForegroundColor Yellow
        Write-Host "  dotnet publish -c Release -p:Platform=$SelectedArchitecture" -ForegroundColor Gray
        exit 1
    }

    Write-Host "[1/4] Creating installation directory..." -ForegroundColor Yellow
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        Write-Host "Created: $InstallPath" -ForegroundColor Green
    } else {
        Write-Host "Directory already exists: $InstallPath" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "[2/4] Copying application files..." -ForegroundColor Yellow
    try {
        # Copy all files from build directory
        Copy-Item -Path "$BuildPath\*" -Destination $InstallPath -Recurse -Force -ErrorAction Stop
        Write-Host "Files copied successfully." -ForegroundColor Green
    } catch {
        Write-Host "ERROR: Failed to copy files - $_" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "[3/4] Creating shortcuts..." -ForegroundColor Yellow
    
    # Create Start Menu folder
    if (-not (Test-Path $StartMenuFolder)) {
        New-Item -ItemType Directory -Path $StartMenuFolder -Force | Out-Null
    }

    # Create Desktop shortcut
    try {
        $WshShell = New-Object -ComObject WScript.Shell
        $DesktopLnk = $WshShell.CreateShortcut($DesktopShortcut)
        $DesktopLnk.TargetPath = "$InstallPath\$AppName"
        $DesktopLnk.WorkingDirectory = $InstallPath
        $DesktopLnk.Description = "ShieldX Professional Antivirus"
        $DesktopLnk.IconLocation = "$InstallPath\$AppName,0"
        $DesktopLnk.Save()
        Write-Host "Created Desktop shortcut" -ForegroundColor Green
    } catch {
        Write-Host "WARNING: Failed to create desktop shortcut - $_" -ForegroundColor Yellow
    }

    # Create Start Menu shortcut
    try {
        $StartMenuLnk = $WshShell.CreateShortcut($StartMenuShortcut)
        $StartMenuLnk.TargetPath = "$InstallPath\$AppName"
        $StartMenuLnk.WorkingDirectory = $InstallPath
        $StartMenuLnk.Description = "ShieldX Professional Antivirus"
        $StartMenuLnk.IconLocation = "$InstallPath\$AppName,0"
        $StartMenuLnk.Save()
        Write-Host "Created Start Menu shortcut" -ForegroundColor Green
    } catch {
        Write-Host "WARNING: Failed to create start menu shortcut - $_" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "[4/4] Registering with Windows..." -ForegroundColor Yellow
    
    # Add to Programs and Features
    try {
        $RegPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX"
        if (-not (Test-Path $RegPath)) {
            New-Item -Path $RegPath -Force | Out-Null
        }
        
        Set-ItemProperty -Path $RegPath -Name "DisplayName" -Value "ShieldX Professional Antivirus"
        Set-ItemProperty -Path $RegPath -Name "DisplayVersion" -Value "3.1.0"
        Set-ItemProperty -Path $RegPath -Name "Publisher" -Value "SYED HAMZA ALI SHAH"
        Set-ItemProperty -Path $RegPath -Name "InstallLocation" -Value $InstallPath
        Set-ItemProperty -Path $RegPath -Name "Architecture" -Value $SelectedArchitecture
        Set-ItemProperty -Path $RegPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$PSScriptRoot\ShieldX_Installer.ps1`" -Uninstall"
        Write-Host "Registered in Programs and Features ($SelectedArchitecture-bit)" -ForegroundColor Green
    } catch {
        Write-Host "WARNING: Failed to register with Windows - $_" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "Installation completed successfully!" -ForegroundColor Green
    Write-Host "ShieldX is installed at: $InstallPath" -ForegroundColor Cyan
    Write-Host "Architecture: $SelectedArchitecture-bit ($RuntimeID)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Starting ShieldX..." -ForegroundColor Yellow
    Start-Process -FilePath "$InstallPath\$AppName"
}

function Uninstall-ShieldX {
    Write-Host "Uninstalling ShieldX Professional Antivirus..." -ForegroundColor Yellow
    
    # Remove shortcuts
    if (Test-Path $DesktopShortcut) {
        Remove-Item $DesktopShortcut -Force -ErrorAction SilentlyContinue
        Write-Host "Removed desktop shortcut" -ForegroundColor Green
    }

    if (Test-Path $StartMenuFolder) {
        Remove-Item $StartMenuFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Removed start menu shortcut" -ForegroundColor Green
    }

    # Remove installation directory
    if (Test-Path $InstallPath) {
        Remove-Item $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Removed application files" -ForegroundColor Green
    }

    # Remove registry entry
    $RegPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ShieldX"
    if (Test-Path $RegPath) {
        Remove-Item $RegPath -Force -ErrorAction SilentlyContinue
        Write-Host "Removed registry entry" -ForegroundColor Green
    }

    Write-Host "Uninstallation completed!" -ForegroundColor Green
}

# Main
if ($Uninstall) {
    Uninstall-ShieldX
} else {
    Install-ShieldX
}

Read-Host "Press Enter to exit"
