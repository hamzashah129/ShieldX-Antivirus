#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Version management script for ShieldX
    
.DESCRIPTION
    Manages version numbers and creates git tags for releases
    
.PARAMETER Action
    Action to perform: "increment", "set", "show", "tag"
    
.PARAMETER Type
    Version component to increment: "major", "minor", "patch"
    
.PARAMETER Version
    Specific version to set (format: X.Y.Z)
    
.EXAMPLE
    .\manage-version.ps1 -Action increment -Type patch
    .\manage-version.ps1 -Action set -Version "3.2.0"
    .\manage-version.ps1 -Action tag
#>

param(
    [ValidateSet("increment", "set", "show", "tag")]
    [string]$Action = "show",
    
    [ValidateSet("major", "minor", "patch")]
    [string]$Type = "patch",
    
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$versionFile = "version.json"

function Get-CurrentVersion {
    $json = Get-Content $versionFile | ConvertFrom-Json
    return $json.version
}

function Update-VersionFile {
    param([string]$NewVersion)
    
    $json = Get-Content $versionFile | ConvertFrom-Json
    $json.version = $NewVersion
    $json | ConvertTo-Json | Set-Content $versionFile
    Write-Host "✅ Version updated to $NewVersion in $versionFile"
}

function Increment-Version {
    param([string]$Version, [string]$Type)
    
    $parts = $Version.Split(".")
    [int]$major = $parts[0]
    [int]$minor = $parts[1]
    [int]$patch = $parts[2]
    
    switch ($Type) {
        "major" { 
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" { 
            $minor++
            $patch = 0
        }
        "patch" { 
            $patch++
        }
    }
    
    return "$major.$minor.$patch"
}

function Show-Version {
    $current = Get-CurrentVersion
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   ShieldX Version Management" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Current Version: $current" -ForegroundColor Cyan
    Write-Host ""
    
    # Show next versions
    $nextPatch = Increment-Version $current "patch"
    $nextMinor = Increment-Version $current "minor"
    $nextMajor = Increment-Version $current "major"
    
    Write-Host "Next Versions:" -ForegroundColor Yellow
    Write-Host "  Patch: $nextPatch"
    Write-Host "  Minor: $nextMinor"
    Write-Host "  Major: $nextMajor"
    Write-Host ""
}

# Main execution
switch ($Action) {
    "show" {
        Show-Version
    }
    
    "increment" {
        $current = Get-CurrentVersion
        $new = Increment-Version $current $Type
        Update-VersionFile $new
        
        Write-Host ""
        Write-Host "Previous: $current"
        Write-Host "New:      $new"
        Write-Host ""
    }
    
    "set" {
        if (-not $Version) {
            Write-Host "❌ Version required for 'set' action" -ForegroundColor Red
            exit 1
        }
        
        if ($Version -notmatch '^\d+\.\d+\.\d+$') {
            Write-Host "❌ Invalid version format. Expected: X.Y.Z" -ForegroundColor Red
            exit 1
        }
        
        Update-VersionFile $Version
    }
    
    "tag" {
        $version = Get-CurrentVersion
        $tag = "v$version"
        
        # Check if tag exists
        $existing = git tag -l $tag 2>$null
        if ($existing) {
            Write-Host "⚠️ Tag $tag already exists" -ForegroundColor Yellow
            exit 1
        }
        
        # Create annotated tag
        git tag -a $tag -m "Release ShieldX v$version"
        Write-Host "✅ Tag created: $tag" -ForegroundColor Green
        Write-Host ""
        Write-Host "To push release to GitHub:"
        Write-Host "  git push origin $tag"
        Write-Host ""
    }
}
