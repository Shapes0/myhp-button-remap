<#
.SYNOPSIS
    HP WMI Hotkey Handler - Custom handler for HP special function keys
.DESCRIPTION
    Monitors HP WMI events (hpqBEvnt) and triggers custom actions for special keyboard keys.
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
    
    # Capture these values in local scope before the action block
    $actionType = $hotkey.ActionType
    $actionValue = $hotkey.ActionValue
    $hotkeyName = $hotkey.Name
    $logFile = $config.LogFile
    $enableLogging = $config.EnableLogging
    
    try {
        # Build WQL query
        $query = "SELECT * FROM hpqBEvnt WHERE EventID = $eventId"
        if ($eventData) {
            $query += " AND EventData = $eventData"
        }
        
        Register-WmiEvent -Namespace "root\wmi" -Query $query -SourceIdentifier $sourceId -Action {
            $eventInfo = $Event.SourceEventArgs.NewEvent
            
            # Use the captured variables with $using: modifier
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            $logMsg = "[$timestamp] [EVENT] $($using:hotkeyName) triggered - EventID: $($eventInfo.EventID), EventData: $($eventInfo.EventData)"
            
            # Log if enabled
            if ($using:enableLogging) {
                try {
                    Add-Content -Path $using:logFile -Value $logMsg -ErrorAction SilentlyContinue
                } catch {}
            }
            
            # Execute the configured action
            try {
                switch ($using:actionType) {
                    "LaunchApplication" {
                        Start-Process $using:actionValue -ErrorAction Stop
                    }
                    "RunCommand" {
                        Invoke-Expression $using:actionValue -ErrorAction Stop
                    }
                    "OpenURL" {
                        Start-Process $using:actionValue -ErrorAction Stop
                    }
                }
            } catch {
                $errorMsg = "[$timestamp] [ERROR] Failed to execute action: $_"
                if ($using:enableLogging) {
                    Add-Content -Path $using:logFile -Value $errorMsg -ErrorAction SilentlyContinue
                }
            }
        }
        
        Write-Log "Registered handler for $($hotkey.Name) - EventID: $eventId (EventData: $eventData) - Action: $actionType"
    } catch {
        Write-Log "Failed to register handler for EventID $eventId : $_" "ERROR"
    }
}

Write-Log "HP Hotkey Handler is now active. Press Ctrl+C to stop."
Write-Host ""
Write-Host "Registered handlers:" -ForegroundColor Cyan
Get-EventSubscriber | Where-Object { $_.SourceIdentifier -like "HPHotkey_*" } | ForEach-Object {
    Write-Host "  - $($_.SourceIdentifier)" -ForegroundColor Green
}
Write-Host ""

# Keep the script running and process events in real-time
try {
    while ($true) { 
        # Wait-Event processes queued events immediately
        Wait-Event -Timeout 1 | Out-Null
        
        # Clear processed events from queue
        Get-Event | Remove-Event
    }
} finally {
    Write-Log "HP Hotkey Handler stopping..."
    # Cleanup event subscriptions
    Get-EventSubscriber | Where-Object { $_.SourceIdentifier -like "HPHotkey_*" } | Unregister-Event
    Write-Log "Cleanup complete. Exiting."
}
