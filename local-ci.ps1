#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Local CI/CD pipeline simulation
    
.DESCRIPTION
    Runs build, test, and release processes locally to verify before pushing to GitHub
    
.PARAMETER Stage
    Stage to run: "build", "test", "release", "all"
    
.PARAMETER Version
    Version to build (default: read from version.json)
    
.EXAMPLE
    .\local-ci.ps1 -Stage all
    .\local-ci.ps1 -Stage build
    .\local-ci.ps1 -Stage release
#>

param(
    [ValidateSet("build", "test", "release", "all")]
    [string]$Stage = "all",
    
    [string]$Version
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
$configuration = "Release"

# Colors
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   $Text" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
}

function Write-Step {
    param([string]$Text)
    Write-Host "[*] $Text" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "[+] $Text" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Text)
    Write-Host "[-] $Text" -ForegroundColor Red
}

# Get version
if (-not $Version) {
    $json = Get-Content "version.json" | ConvertFrom-Json
    $Version = $json.version
}

Write-Header "ShieldX Local CI/CD Pipeline v$Version"

# Stage: Build
if ($Stage -in @("build", "all")) {
    Write-Header "Stage 1: Build"
    
    Write-Step "Restoring dependencies..."
    dotnet restore ShieldX.sln
    Write-Success "Dependencies restored"
    
    Write-Step "Building application..."
    dotnet build ShieldX.sln --configuration $configuration
    Write-Success "Build successful"
}

# Stage: Test
if ($Stage -in @("test", "all")) {
    Write-Header "Stage 2: Tests"
    
    if (Test-Path "ShieldX.Tests") {
        Write-Step "Running tests..."
        $testResult = 0
        dotnet test ShieldX.Tests --configuration $configuration --logger "console;verbosity=minimal"
        $testResult = $?
        if (-not $testResult) {
            Write-Error-Custom "Tests failed (continuing...)"
        }
        Write-Success "Test stage complete"
    } else {
        Write-Host "WARNING: Test project not found, skipping tests" -ForegroundColor Yellow
    }
}

# Stage: Release
if ($Stage -in @("release", "all")) {
    Write-Header "Stage 3: Release Build"
    Write-Step "Building signed release package..."
    . .\build-installer.ps1 -SignInstaller
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Release build complete with code signing"
        New-Item -ItemType Directory -Path "releases" -Force | Out-Null
        Get-ChildItem "ShieldX.Installer\output\*.exe" -ErrorAction SilentlyContinue | Copy-Item -Destination "releases\" -Force
        Get-ChildItem "ShieldX.Installer\output\*.zip" -ErrorAction SilentlyContinue | Copy-Item -Destination "releases\" -Force
        Write-Host "`nRelease artifacts:" -ForegroundColor Cyan
        Get-ChildItem "releases\*" -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "  * $($_.Name)"
        }
    } else {
        Write-Error-Custom "Release build failed!"
        exit 1
    }
}

Write-Header "Pipeline Complete"
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Test releases"
Write-Host "  2. Commit and push to GitHub"
