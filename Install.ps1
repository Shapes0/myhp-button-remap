<#
.SYNOPSIS
    Installer for HP Button Remap
.DESCRIPTION
    Installs the HP Button Remap tray application to Startup folder
#>

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Installer ===" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = $PSScriptRoot

# Paths
$installDir = Join-Path $env:LOCALAPPDATA "HPButtonRemap"
$appExe = Join-Path $scriptDir "HPButtonRemap.exe"
$configExe = Join-Path $scriptDir "HPButtonRemapConfig.exe"
$configJson = Join-Path $scriptDir "config.json"
$startupFolder = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupFolder "HP Button Remap.lnk"

# Validate files exist
if (-not (Test-Path $appExe)) {
    Write-Host "[ERROR] Application executable not found: $appExe" -ForegroundColor Red
    Write-Host "Building from source..." -ForegroundColor Yellow
    
    # Try to build from source
    $projectPath = Join-Path $scriptDir "HPButtonRemap\HPButtonRemap.csproj"
    if (Test-Path $projectPath) {
        Write-Host "[INFO] Building HPButtonRemap..." -ForegroundColor Cyan
        dotnet publish $projectPath --configuration Release --output $scriptDir --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -r win-x64
        
        if (-not (Test-Path $appExe)) {
            Write-Host "[ERROR] Build failed!" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "[ERROR] Please ensure you have the release package" -ForegroundColor Red
        exit 1
    }
}

Write-Host "[INFO] Installing to: $installDir" -ForegroundColor Cyan

# Create installation directory
if (-not (Test-Path $installDir)) {
    New-Item -Path $installDir -ItemType Directory -Force | Out-Null
    Write-Host "[INFO] Created installation directory" -ForegroundColor Green
}

# Copy files
Write-Host "[INFO] Copying files..." -ForegroundColor Cyan
Copy-Item $appExe -Destination $installDir -Force
Write-Host "  - HPButtonRemap.exe" -ForegroundColor Gray

if (Test-Path $configExe) {
    Copy-Item $configExe -Destination $installDir -Force
    Write-Host "  - HPButtonRemapConfig.exe" -ForegroundColor Gray
}

if (Test-Path $configJson) {
    $destConfig = Join-Path $installDir "config.json"
    if (-not (Test-Path $destConfig)) {
        Copy-Item $configJson -Destination $installDir -Force
        Write-Host "  - config.json" -ForegroundColor Gray
    } else {
        Write-Host "  - config.json (existing config preserved)" -ForegroundColor Yellow
    }
}

# Create startup shortcut
Write-Host "[INFO] Creating startup shortcut..." -ForegroundColor Cyan
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = Join-Path $installDir "HPButtonRemap.exe"
$Shortcut.WorkingDirectory = $installDir
$Shortcut.Description = "HP Button Remap - Monitors HP special function keys"
$Shortcut.Save()
Write-Host "[INFO] Startup shortcut created" -ForegroundColor Green

# Create Start Menu shortcut for configurator (if exists)
if (Test-Path (Join-Path $installDir "HPButtonRemapConfig.exe")) {
    Write-Host "[INFO] Creating Start Menu shortcut..." -ForegroundColor Cyan
    $startMenuFolder = [Environment]::GetFolderPath("Programs")
    $startMenuShortcut = Join-Path $startMenuFolder "HP Button Remap Configurator.lnk"
    
    $Shortcut = $WshShell.CreateShortcut($startMenuShortcut)
    $Shortcut.TargetPath = Join-Path $installDir "HPButtonRemapConfig.exe"
    $Shortcut.WorkingDirectory = $installDir
    $Shortcut.Description = "HP Button Remap Configurator"
    $Shortcut.Save()
    Write-Host "[INFO] Start Menu shortcut created" -ForegroundColor Green
}

# Start the application
Write-Host "[INFO] Starting HP Button Remap..." -ForegroundColor Cyan
Start-Process (Join-Path $installDir "HPButtonRemap.exe") -WorkingDirectory $installDir

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "The HP Button Remap tray application is now running!" -ForegroundColor Green
Write-Host ""
Write-Host "Key Points:" -ForegroundColor Cyan
Write-Host "  - Application installed to: $installDir" -ForegroundColor Gray
Write-Host "  - Tray icon visible in system tray (right-click for options)" -ForegroundColor Gray
Write-Host "  - Automatically starts when you log in" -ForegroundColor Gray
Write-Host "  - Configuration file: $installDir\config.json" -ForegroundColor Gray
if (Test-Path (Join-Path $installDir "HPButtonRemapConfig.exe")) {
    Write-Host "  - Open 'HP Button Remap Configurator' from Start Menu to configure" -ForegroundColor Gray
}
Write-Host ""
Write-Host "To uninstall, run Uninstall.ps1" -ForegroundColor Yellow
Write-Host ""
