<#
.SYNOPSIS
    HP WMI Hotkey Handler - Custom handler for HP special function keys
.DESCRIPTION
    Monitors HP WMI events (hpqBEvnt) and triggers custom actions for special keyboard keys.
    Designed for HP laptops with special function keys that don't generate standard scancodes.
.NOTES
    Author: Shapes0
    Repository: https://github.com/Shapes0/myhp-button-remap
#>

[CmdletBinding()]
param()

# Load configuration
$configPath = Join-Path $PSScriptRoot "config.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Host "[INFO] Loaded configuration from $configPath" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Configuration file not found: $configPath" -ForegroundColor Red
    exit 1
}

# Logging function
function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage
    if ($config.EnableLogging) {
        Add-Content -Path $config.LogFile -Value $logMessage
    }
}

Write-Log "HP Hotkey Handler starting..."

# Register event handlers for each configured hotkey
foreach ($hotkey in $config.Hotkeys) {
    $eventId = $hotkey.EventID
    $eventData = $hotkey.EventData
    $sourceId = "HPHotkey_$eventId"
    
    try {
        # Build WQL query
        $query = "SELECT * FROM hpqBEvnt WHERE EventID = $eventId"
        if ($eventData) {
            $query += " AND EventData = $eventData"
        }
        
        Register-WmiEvent -Namespace "root\wmi" -Query $query -SourceIdentifier $sourceId -Action {
            $eventInfo = $Event.SourceEventArgs.NewEvent
            $hotkeyConfig = $using:hotkey
            
            # Log the event
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            $logMsg = "[$timestamp] [EVENT] Hotkey triggered - EventID: $($eventInfo.EventID), EventData: $($eventInfo.EventData)"
            
            if ($using:config.EnableLogging) {
                Add-Content -Path $using:config.LogFile -Value $logMsg
            }
            
            # Execute the configured action
            switch ($hotkeyConfig.ActionType) {
                "LaunchApplication" {
                    Start-Process $hotkeyConfig.ActionValue -ErrorAction SilentlyContinue
                }
                "RunCommand" {
                    Invoke-Expression $hotkeyConfig.ActionValue -ErrorAction SilentlyContinue
                }
                "OpenURL" {
                    Start-Process $hotkeyConfig.ActionValue -ErrorAction SilentlyContinue
                }
            }
        }
        
        Write-Log "Registered handler for EventID $eventId (EventData: $eventData) - Action: $($hotkey.ActionType)"
    } catch {
        Write-Log "Failed to register handler for EventID $eventId : $_" "ERROR"
    }
}

Write-Log "HP Hotkey Handler is now active. Press Ctrl+C to stop."

# Keep the script running
try {
    while ($true) { 
        Start-Sleep -Seconds 60
    }
} finally {
    Write-Log "HP Hotkey Handler stopping..."
    # Cleanup event subscriptions
    Get-EventSubscriber | Where-Object { $_.SourceIdentifier -like "HPHotkey_*" } | Unregister-Event
    Write-Log "Cleanup complete. Exiting."
}
