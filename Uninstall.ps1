<#
.SYNOPSIS
    Uninstaller for HP Button Remap
.DESCRIPTION
    Removes the HP Button Remap Windows Service and configurator
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Uninstaller ===" -ForegroundColor Cyan
Write-Host ""

$serviceName = "HPButtonRemapService"
$installDir = Join-Path $env:ProgramFiles "HPButtonRemap"
$startMenuPath = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuPath "HP Button Remap Configurator.lnk"

# Stop and remove the service
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "[INFO] Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    
    Write-Host "[INFO] Removing service..." -ForegroundColor Yellow
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "[OK] Service removed" -ForegroundColor Green
} else {
    Write-Host "[INFO] Service not found" -ForegroundColor Yellow
}

# Remove Start Menu shortcut
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
    Write-Host "[OK] Removed Start Menu shortcut" -ForegroundColor Green
}

# Remove installation directory
if (Test-Path $installDir) {
    Write-Host "[INFO] Removing installation directory..." -ForegroundColor Yellow
    
    $response = Read-Host "Do you want to keep your configuration file? (Y/N)"
    if ($response -eq 'Y' -or $response -eq 'y') {
        $backupPath = Join-Path $env:USERPROFILE "Downloads\config.json.backup"
        Copy-Item (Join-Path $installDir "config.json") -Destination $backupPath -ErrorAction SilentlyContinue
        Write-Host "[OK] Configuration backed up to: $backupPath" -ForegroundColor Green
    }
    
    Remove-Item $installDir -Recurse -Force
    Write-Host "[OK] Installation directory removed" -ForegroundColor Green
}

Write-Host ""
Write-Host "[SUCCESS] Uninstallation complete!" -ForegroundColor Green
Write-Host ""
