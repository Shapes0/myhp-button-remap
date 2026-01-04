<#
.SYNOPSIS
    Uninstaller for HP Button Remap - Native Windows Application
.DESCRIPTION
    Removes the scheduled task and stops the application.
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Uninstaller ===" -ForegroundColor Cyan
Write-Host ""

$taskName = "HP-Button-Remap"

# Stop any running instances
$processes = Get-Process -Name "HPButtonRemap" -ErrorAction SilentlyContinue
if ($processes) {
    Write-Host "[INFO] Stopping running instances..." -ForegroundColor Yellow
    $processes | Stop-Process -Force
    Write-Host "[OK] Stopped running instances" -ForegroundColor Green
}

# Remove scheduled task
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($task) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Host "[OK] Removed scheduled task: $taskName" -ForegroundColor Green
} else {
    Write-Host "[INFO] Scheduled task not found: $taskName" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[SUCCESS] Uninstallation complete!" -ForegroundColor Green
