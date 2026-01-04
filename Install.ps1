<#
.SYNOPSIS
    Installer for HP Button Remap
.DESCRIPTION
    Installs the HP Button Remap as a Windows Service with GUI configurator
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Installer ===" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = $PSScriptRoot
$serviceName = "HPButtonRemapService"
$serviceDisplayName = "HP Button Remap Service"
$serviceDescription = "Monitors HP WMI events and executes configured actions when special HP laptop buttons are pressed"

# Paths
$installDir = Join-Path $env:ProgramFiles "HPButtonRemap"
$serviceExe = Join-Path $scriptDir "HPButtonRemap.exe"
$configExe = Join-Path $scriptDir "HPButtonRemapConfig.exe"
$configJson = Join-Path $scriptDir "config.json"

# Validate files exist
if (-not (Test-Path $serviceExe)) {
    Write-Host "[ERROR] Service executable not found: $serviceExe" -ForegroundColor Red
    Write-Host "Please ensure you have the release package" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $configExe)) {
    Write-Host "[ERROR] Configurator executable not found: $configExe" -ForegroundColor Red
    exit 1
}

Write-Host "[INFO] Installing to: $installDir" -ForegroundColor Cyan

# Stop and remove existing service if it exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "[INFO] Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    
    Write-Host "[INFO] Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

# Create installation directory
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

# Copy files
Write-Host "[INFO] Copying files..." -ForegroundColor Cyan
Copy-Item $serviceExe -Destination $installDir -Force
Copy-Item $configExe -Destination $installDir -Force

# Copy all DLL dependencies
Get-ChildItem -Path $scriptDir -Filter "*.dll" | ForEach-Object {
    Copy-Item $_.FullName -Destination $installDir -Force
}

# Copy config file
if (Test-Path $configJson) {
    Copy-Item $configJson -Destination $installDir -Force
} else {
    # Create default config
    $defaultConfig = @"
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Launch Notepad",
      "EventID": 29,
      "EventData": 8616,
      "Type": "LaunchApp",
      "LaunchPath": "notepad.exe",
      "LaunchArguments": ""
    }
  ]
}
"@
    Set-Content -Path (Join-Path $installDir "config.json") -Value $defaultConfig
}

# Install the service running as LocalSystem
# The service will use Windows APIs to launch processes in the user's session
Write-Host "[INFO] Installing Windows Service..." -ForegroundColor Cyan
$serviceExePath = Join-Path $installDir "HPButtonRemap.exe"

New-Service -Name $serviceName `
    -DisplayName $serviceDisplayName `
    -Description $serviceDescription `
    -BinaryPathName $serviceExePath `
    -StartupType Automatic | Out-Null

# Start the service
Write-Host "[INFO] Starting service..." -ForegroundColor Cyan
Start-Service -Name $serviceName

# Create Start Menu shortcut for configurator
$startMenuPath = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuPath "HP Button Remap Configurator.lnk"
$configExePath = Join-Path $installDir "HPButtonRemapConfig.exe"

$WScriptShell = New-Object -ComObject WScript.Shell
$shortcut = $WScriptShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $configExePath
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Configure HP Button Remap actions"
$shortcut.Save()

Write-Host ""
Write-Host "[SUCCESS] Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Service Status:" -ForegroundColor Cyan
Get-Service -Name $serviceName | Select-Object Name, Status, StartType | Format-List
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  - Config File: $installDir\config.json" -ForegroundColor White
Write-Host "  - Configurator: Search for 'HP Button Remap' in Start Menu" -ForegroundColor White
Write-Host ""
Write-Host "The service is now running and will launch applications in your user session." -ForegroundColor Cyan
Write-Host "To configure button actions, run the configurator from the Start Menu" -ForegroundColor Cyan
Write-Host ""
