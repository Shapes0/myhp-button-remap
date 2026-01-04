<#
.SYNOPSIS
    Installer for HP Button Remap - Native Windows Application
.DESCRIPTION
    Creates a scheduled task to run the native application at user logon.
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "=== HP Button Remap Installer ===" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = $PSScriptRoot

# Check for prebuilt executable first (from release download)
$prebuiltPath = Join-Path $scriptDir "HPButtonRemap.exe"
$builtPath = Join-Path $scriptDir "HPButtonRemap\bin\Release\net8.0-windows\HPButtonRemap.exe"
$configFile = Join-Path $scriptDir "config.json"

# Determine which executable to use
$appPath = $null
if (Test-Path $prebuiltPath) {
    Write-Host "[INFO] Using prebuilt executable" -ForegroundColor Cyan
    $appPath = $prebuiltPath
} elseif (Test-Path $builtPath) {
    Write-Host "[INFO] Using locally built executable" -ForegroundColor Cyan
    $appPath = $builtPath
} else {
    # Try to build from source
    Write-Host "[INFO] Executable not found. Building from source..." -ForegroundColor Yellow
    $projectPath = Join-Path $scriptDir "HPButtonRemap"
    
    if (Test-Path $projectPath) {
        Push-Location $projectPath
        try {
            dotnet build --configuration Release
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed"
            }
            $appPath = $builtPath
        } finally {
            Pop-Location
        }
    } else {
        Write-Host "[ERROR] Cannot find prebuilt executable or source code" -ForegroundColor Red
        Write-Host "Please download the release package from GitHub or clone the repository" -ForegroundColor Red
        exit 1
    }
}

# Validate files exist
if (-not (Test-Path $appPath)) {
    Write-Host "[ERROR] Application not found: $appPath" -ForegroundColor Red
    Write-Host "Please build the application first using 'dotnet build' in the HPButtonRemap directory" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $configFile)) {
    Write-Host "[ERROR] Configuration file not found: $configFile" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Found required files" -ForegroundColor Green

# Create scheduled task
$taskName = "HP-Button-Remap"
$taskDescription = "Native Windows application for remapping HP laptop special function keys"

# Remove existing task if present
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "[INFO] Removing existing scheduled task..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

# Create new task
$action = New-ScheduledTaskAction `
    -Execute $appPath `
    -WorkingDirectory $scriptDir

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
Write-Host "The application will start automatically at next logon." -ForegroundColor Cyan
Write-Host "To start it now, run: Start-ScheduledTask -TaskName '$taskName'" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration file: $configFile" -ForegroundColor Yellow
Write-Host "Edit this file to customize your button actions." -ForegroundColor Yellow
Write-Host ""
