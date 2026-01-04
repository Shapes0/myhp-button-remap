<#
.SYNOPSIS
    Uninstaller for HP Button Remap - Startup Folder Method
.DESCRIPTION
    Removes the shortcut from the Startup folder and stops the application.
#>

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Uninstaller (Startup Folder) ===" -ForegroundColor Cyan
Write-Host ""

# Get paths
$scriptDir = $PSScriptRoot
$startupFolder = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupFolder "HP Button Remap.lnk"
$vbsScriptPath = Join-Path $scriptDir "HPButtonRemap-Hidden.vbs"

# Stop any running instances
$processes = Get-Process -Name "HPButtonRemap" -ErrorAction SilentlyContinue
if ($processes) {
    Write-Host "[INFO] Stopping running instances..." -ForegroundColor Yellow
    $processes | Stop-Process -Force
    Write-Host "[OK] Stopped running instances" -ForegroundColor Green
}

# Remove shortcut from startup folder
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
    Write-Host "[OK] Removed startup shortcut: $shortcutPath" -ForegroundColor Green
} else {
    Write-Host "[INFO] Startup shortcut not found: $shortcutPath" -ForegroundColor Yellow
}

# Remove VBS helper script
if (Test-Path $vbsScriptPath) {
    Remove-Item $vbsScriptPath -Force
    Write-Host "[OK] Removed VBS helper script" -ForegroundColor Green
}

Write-Host ""
Write-Host "[SUCCESS] Uninstallation complete!" -ForegroundColor Green
Write-Host ""
