<#
.SYNOPSIS
    HP WMI Hotkey Handler - Launch apps with special HP function keys
.DESCRIPTION
    Monitors HP WMI events (hpqBEvnt) and launches configured applications when special keys are pressed.
    Designed for HP laptops with special function keys that don't generate standard keyboard scancodes.
.NOTES
    Repository: https://github.com/Shapes0/myhp-button-remap
    License: MIT
#>

[CmdletBinding()]
param()

# Load configuration
$configPath = Join-Path $PSScriptRoot "config.json"
if (-not (Test-Path $configPath)) {
    Write-Host "[ERROR] Configuration file not found: $configPath" -ForegroundColor Red
    exit 1
}

$config = Get-Content $configPath | ConvertFrom-Json

Write-Host "HP Hotkey Handler starting..." -ForegroundColor Green

# Register handlers for each configured hotkey
foreach ($hotkey in $config.Hotkeys) {
    $eventId = $hotkey.EventID
    $eventData = $hotkey.EventData
    $command = $hotkey.Command
    $sourceId = "HPHotkey_$eventId"
    
    # Build WQL query with EventData filter if specified
    $query = "SELECT * FROM hpqBEvnt WHERE EventID = $eventId"
    if ($eventData) {
        $query += " AND EventData = $eventData"
    }
    
    # Create action script block with Start-Process and error suppression
    $actionScript = [ScriptBlock]::Create("Start-Process '$command' -ErrorAction SilentlyContinue")
    
    try {
        Register-WmiEvent -Namespace "root\wmi" -Query $query -SourceIdentifier $sourceId -Action $actionScript -ErrorAction Stop
        Write-Host "Registered: $($hotkey.Name) -> $command (EventID: $eventId, EventData: $eventData)" -ForegroundColor Cyan
    } catch {
        Write-Host "[ERROR] Failed to register $($hotkey.Name): $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Active. Press Ctrl+C to stop." -ForegroundColor Green
Write-Host ""

# Keep the script running and clean event queue
try {
    while ($true) { 
        Wait-Event -Timeout 1 | Out-Null
        Get-Event | Remove-Event
    }
} finally {
    Write-Host "Stopping and cleaning up..." -ForegroundColor Yellow
    Get-EventSubscriber | Where-Object { $_.SourceIdentifier -like "HPHotkey_*" } | Unregister-Event
    Write-Host "Stopped." -ForegroundColor Green
}
