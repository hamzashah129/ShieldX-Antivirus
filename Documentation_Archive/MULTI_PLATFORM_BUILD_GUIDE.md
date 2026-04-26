# ShieldX Multi-Platform Build Guide

This guide explains how to build ShieldX for both **32-bit (x86)** and **64-bit (x64)** Windows systems.

## Quick Start

### Build for Current System (Auto-detected)
```powershell
# Automatic detection based on your system
dotnet publish -c Release
```

### Build Specific Architecture

#### Build 64-bit (x64) Version
```powershell
dotnet publish -c Release -p:Platform=x64
```

#### Build 32-bit (x86) Version
```powershell
dotnet publish -c Release -p:Platform=x86
```

### Build Both Architectures (Full Release)
```powershell
# Clean previous builds
dotnet clean

# Build x64
dotnet publish -c Release -p:Platform=x64

# Build x86
dotnet publish -c Release -p:Platform=x86
```

## Build Output Locations

After building, the compiled executables will be located at:

- **64-bit**: `bin\Release\net8.0-windows\win-x64\ShieldX.exe`
- **32-bit**: `bin\Release\net8.0-windows\win-x86\ShieldX.exe`

## Installation

### Automatic Installation (Recommended)
The installer automatically detects your system architecture and installs the appropriate version:

```powershell
# Run as Administrator
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\ShieldX_Installer.ps1
```

The installer will:
1. Auto-detect if your system is 32-bit or 64-bit
2. Look for the corresponding build files
3. Install the appropriate version
4. Register with Windows Programs and Features

### Manual Installation by Architecture

#### Install 32-bit Version
```powershell
# Ensure you've built the x86 version first
dotnet publish -c Release -p:Platform=x86
.\ShieldX_Installer.ps1
```

#### Install 64-bit Version
```powershell
# Ensure you've built the x64 version first
dotnet publish -c Release -p:Platform=x64
.\ShieldX_Installer.ps1
```

## Project Configuration

The project supports both architectures through conditional platform targeting:

### Configuration in ShieldX.csproj:
```xml
<Platforms>x86;x64</Platforms>

<!-- x64 Configuration -->
<PropertyGroup Condition="'$(Platform)'=='x64'">
  <PlatformTarget>x64</PlatformTarget>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>

<!-- x86 Configuration -->
<PropertyGroup Condition="'$(Platform)'=='x86'">
  <PlatformTarget>x86</PlatformTarget>
  <RuntimeIdentifier>win-x86</RuntimeIdentifier>
</PropertyGroup>
```

## Using Visual Studio

### Build for Specific Platform in Visual Studio

1. Open `ShieldX.sln` in Visual Studio
2. In Solution Explorer, right-click the project
3. Select **Build** > **Publish Profile** > Select desired platform
4. Or use the Configuration Manager:
   - Go to **Build** > **Configuration Manager**
   - Change the "Active solution platform" to **x64** or **x86**
   - Build or Publish

### Create Release with Both Architectures

1. Build x64: Select **Release | x64** configuration, then Publish
2. Build x86: Select **Release | x86** configuration, then Publish

## Troubleshooting

### Installer Says "Build files not found"

**Error:**
```
ERROR: Build files not found for 64-bit (x64) at:
  C:\Users\...\bin\Release\net8.0-windows\win-x64
```

**Solution:**
Make sure you've built the version for your system architecture:

```powershell
# For 64-bit system
dotnet publish -c Release -p:Platform=x64

# For 32-bit system
dotnet publish -c Release -p:Platform=x86

# Or build both
dotnet publish -c Release -p:Platform=x64
dotnet publish -c Release -p:Platform=x86
```

### Build Fails with Platform Error

If you see errors like `Unknown platform 'x64'`, ensure you've saved the `.csproj` file and reload the solution:

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet publish -c Release -p:Platform=x64
```

## System Requirements

### 32-bit Systems
- Windows 7 or later (32-bit)
- .NET 8.0 Runtime (32-bit)
- 256 MB RAM minimum

### 64-bit Systems
- Windows Vista or later (64-bit)
- .NET 8.0 Runtime (64-bit)
- 512 MB RAM minimum

## Version Information

- **ShieldX Version**: 3.1.0
- **.NET Framework**: .NET 8.0
- **Supported Architectures**: x86 (32-bit), x64 (64-bit)
- **Supported Windows**: Windows 7 SP1 and later

## Verification

After installation, verify the correct architecture is installed:

**Windows 11/10:**
1. Go to **Settings** > **Apps** > **Installed apps**
2. Find "ShieldX Professional Antivirus"
3. Check the installation location

**All Versions:**
1. Open Task Manager (Ctrl+Shift+Esc)
2. Find "ShieldX.exe" in the process list
3. Right-click > Properties > Details
4. Check the Image path shows the correct architecture folder

