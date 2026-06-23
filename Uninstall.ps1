<#
.SYNOPSIS
    Lightweight uninstaller for HP Button Remap
.DESCRIPTION
    Removes startup registration and installed files.
#>

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Lightweight Uninstaller ===" -ForegroundColor Cyan
Write-Host ""

# Paths
$installDir = Join-Path $env:LOCALAPPDATA "HPButtonRemap"
$startupFolder = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupFolder "HP Button Remap.lnk"

# Stop the running application
Write-Host "[INFO] Stopping HP Button Remap..." -ForegroundColor Cyan
Get-Process -Name "HPButtonRemap" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Remove startup shortcut
if (Test-Path $shortcutPath) {
    Write-Host "[INFO] Removing startup shortcut..." -ForegroundColor Cyan
    Remove-Item $shortcutPath -Force
    Write-Host "[INFO] Startup shortcut removed" -ForegroundColor Green
}

# Ask user about config
$keepConfig = Read-Host "Keep configuration file? (Y/n)"
$removeConfig = ($keepConfig -eq "n" -or $keepConfig -eq "N")

# Remove installation directory
if (Test-Path $installDir) {
    Write-Host "[INFO] Removing installation directory..." -ForegroundColor Cyan
    
    if ($removeConfig) {
        Remove-Item $installDir -Recurse -Force
        Write-Host "[INFO] Installation directory removed" -ForegroundColor Green
    } else {
        # Backup config
        $configPath = Join-Path $installDir "config.json"
        if (Test-Path $configPath) {
            $backupPath = Join-Path $env:USERPROFILE "Desktop\HPButtonRemap-config-backup.json"
            Copy-Item $configPath -Destination $backupPath -Force
            Write-Host "[INFO] Configuration backed up to: $backupPath" -ForegroundColor Yellow
        }
        
        Remove-Item $installDir -Recurse -Force
        Write-Host "[INFO] Installation directory removed (config backed up)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Uninstallation Complete ===" -ForegroundColor Green
Write-Host ""
if (-not $removeConfig) {
    Write-Host "Your configuration has been backed up to your Desktop" -ForegroundColor Yellow
}
Write-Host ""
