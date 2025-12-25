<#
.SYNOPSIS
    Installer for HP WMI Hotkey Handler
.DESCRIPTION
    Creates a scheduled task to run the hotkey handler at user logon.
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "=== HP WMI Hotkey Handler Installer ===" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = $PSScriptRoot
$handlerScript = Join-Path $scriptDir "HP-HotkeyHandler.ps1"
$configFile = Join-Path $scriptDir "config.json"

# Validate files exist
if (-not (Test-Path $handlerScript)) {
    Write-Host "[ERROR] Handler script not found: $handlerScript" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $configFile)) {
    Write-Host "[ERROR] Configuration file not found: $configFile" -ForegroundColor Red
    exit 1
}

# Create log directory, uncomment if needed
<#
$config = Get-Content $configFile | ConvertFrom-Json
$logDir = Split-Path $config.LogFile -Parent
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    Write-Host "[OK] Created log directory: $logDir" -ForegroundColor Green
}
#>

# Create scheduled task
$taskName = "HP-WMI-Hotkey-Handler"
$taskDescription = "Custom handler for HP special function keys via WMI events"

# Remove existing task if present
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "[INFO] Removing existing scheduled task..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

# Create new task
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-WindowStyle Hidden -ExecutionPolicy Bypass -NoProfile -File `"$handlerScript`""

$trigger = New-ScheduledTaskTrigger -AtLogon -User $env:USERNAME

$principal = New-ScheduledTaskPrincipal `
    -UserId "$env:USERDOMAIN\$env:USERNAME" `
    -LogonType Interactive `
    -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask `
    -TaskName $taskName `
    -Description $taskDescription `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings | Out-Null

Write-Host ""
Write-Host "[SUCCESS] Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "The handler will start automatically at next logon." -ForegroundColor Cyan
Write-Host "To start it now, run: Start-ScheduledTask -TaskName '$taskName'" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration file: $configFile" -ForegroundColor Yellow
Write-Host "Edit this file to customize your hotkey actions." -ForegroundColor Yellow
