<#
.SYNOPSIS
    Uninstaller for HP WMI Hotkey Handler
.DESCRIPTION
    Removes the scheduled task and stops any running handlers.
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "=== HP WMI Hotkey Handler Uninstaller ===" -ForegroundColor Cyan
Write-Host ""

$taskName = "HP-WMI-Hotkey-Handler"

# Stop running handlers
Get-EventSubscriber | Where-Object { $_.SourceIdentifier -like "HPHotkey_*" } | ForEach-Object {
    Unregister-Event -SourceIdentifier $_.SourceIdentifier -Force
    Write-Host "[OK] Stopped event handler: $($_.SourceIdentifier)" -ForegroundColor Green
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
