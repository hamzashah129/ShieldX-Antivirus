param(
    [string]$Version = "3.1.1",
    [string]$CertPath,
    [string]$CertPassword,
    [switch]$SignInstaller
)
$ErrorActionPreference = "Stop"

# ── PATHS ── edit Root if your folder is different
$Root         = $PSScriptRoot
$AppCsproj    = "$Root\ShieldX.csproj"
$InstallerDir = "$Root\ShieldX.Installer"
$PayloadDir   = "$InstallerDir\payload"
$PayloadZip   = "$PayloadDir\ShieldX_payload.zip"
$OutputDir    = "$InstallerDir\output"
$StagingDir   = "$InstallerDir\staging"
$CertDir      = "$Root\certificates"

Write-Host ""
Write-Host "========================================" -f Cyan
Write-Host "   ShieldX Standalone Installer Build  " -f Cyan
Write-Host "========================================" -f Cyan
Write-Host ""

# ── CLEAN ──────────────────────────────────────────────────
Write-Host "[1/5] Cleaning..." -f Yellow
@("$Root\obj", "$InstallerDir\obj",
  $StagingDir, $PayloadDir) | ForEach-Object {
    if (Test-Path $_) { Remove-Item $_ -Recurse -Force }
    New-Item $_ -ItemType Directory -Force | Out-Null
}
New-Item $OutputDir -ItemType Directory -Force | Out-Null

# ── PUBLISH MAIN APP (self-contained) ──────────────────────
Write-Host "[2/5] Publishing ShieldX (self-contained)..." -f Yellow

if (-not (Test-Path $AppCsproj)) {
    Write-Host "ERROR: ShieldX.csproj not found at $AppCsproj" -f Red
    exit 1
}

dotnet publish $AppCsproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:TrimmerRootDescriptor="" `
    -o $StagingDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "PUBLISH FAILED!" -f Red; exit 1
}

# Verify ShieldX.exe
$exePath = "$StagingDir\ShieldX.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ShieldX.exe missing from staging!" -f Red
    Get-ChildItem $StagingDir | Format-Table Name, Length
    exit 1
}

$exeMB = [math]::Round(
    (Get-Item $exePath).Length / 1MB, 1)
Write-Host "  ShieldX.exe: $exeMB MB (self-contained)" -f Green

# ── CREATE PAYLOAD ZIP ─────────────────────────────────────
Write-Host "[3/5] Creating payload ZIP..." -f Yellow
if (Test-Path $PayloadZip) { Remove-Item $PayloadZip -Force }

Compress-Archive `
    -Path "$StagingDir\*" `
    -DestinationPath $PayloadZip `
    -CompressionLevel Optimal

$zipMB = [math]::Round(
    (Get-Item $PayloadZip).Length / 1MB, 1)
Write-Host "  Payload ZIP: $zipMB MB" -f Green

# Verify ShieldX.exe is inside ZIP
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [IO.Compression.ZipFile]::OpenRead($PayloadZip)
$ok = $z.Entries | Where-Object { $_.Name -eq "ShieldX.exe" }
$count = $z.Entries.Count
$z.Dispose()

if (-not $ok) {
    Write-Host "ShieldX.exe missing from ZIP!" -f Red
    exit 1
}
Write-Host "  Verified: ShieldX.exe in ZIP ($count files)" -f Green

# ── BUILD WPF INSTALLER ────────────────────────────────────
Write-Host "[4/5] Building installer..." -f Yellow
# Note: Skipping WPF installer project (not present in this build)
# Using payload ZIP as distribution artifact

# ── COPY TO OUTPUT ─────────────────────────────────────────
Write-Host "[5/5] Finalizing..." -f Yellow
if (-not (Test-Path $OutputDir)) {
    New-Item $OutputDir -ItemType Directory -Force | Out-Null
}

# Copy ZIP to output
$finalZip = "$OutputDir\ShieldX_Professional_v${Version}.zip"
if (Test-Path $finalZip) { Remove-Item $finalZip -Force }
Copy-Item $PayloadZip $finalZip

$finalMB = [math]::Round(
    (Get-Item $finalZip).Length / 1MB, 1)
Write-Host "  Release package: $finalMB MB (ZIP)" -f Green

# Cleanup temp dirs
Remove-Item $StagingDir -Recurse -Force -EA SilentlyContinue

# ── CODE SIGNING ─────────────────────────────────────────────
if ($SignInstaller -or $CertPath) {
    Write-Host ""
    Write-Host "[6/5] Signing installer..." -f Yellow
    
    if (-not $CertPath) {
        Write-Host "Looking for certificate..." -f White
        $pfxFiles = Get-ChildItem "$CertDir\*.pfx" -ErrorAction SilentlyContinue
        if ($pfxFiles) {
            $CertPath = $pfxFiles[0].FullName
            $CertPassword = "ShieldX@2026"
            Write-Host "  Found: $($pfxFiles[0].Name)" -f Green
        }
    }
    
    if ($CertPath -and $CertPassword) {
        if (-not (Test-Path $CertPath)) {
            Write-Host "[-] Certificate not found: $CertPath" -f Red
        }
        else {
            Write-Host "  [!] Code signing: ZIP files cannot be authenticode signed." -f Yellow
            Write-Host "      Consider building EXE installer for signed distribution." -f Yellow
        }
    }
    else {
        Write-Host "[!] Code signing requested but no certificate provided" -f Yellow
    }
}

Write-Host ""
Write-Host "========================================" -f Green
Write-Host "         BUILD SUCCESSFUL              " -f Green
Write-Host "========================================" -f Green
Write-Host "  Output : $finalZip" -f Cyan
Write-Host "  Size   : $finalMB MB" -f Cyan
Write-Host ""
Write-Host "NOTE: Self-contained application package." -f Yellow
Write-Host "Extract and run ShieldX.exe to launch." -f Yellow
Write-Host ""
Write-Host "Run as administrator" -f Green
