# myhp-button-remap

Remap F11 key MyHP/HP System Event Utility button

After some back and forth with Claude I was able to figure this out. This is a rudimentary script, use at your own risk.

Custom handler for HP laptop special function keys that don't generate standard keyboard scancodes.

## Problem

HP laptops have special function keys (like F11 with myHP icon) that trigger WMI events rather than standard keyboard input. These keys are invisible to tools like PowerToys Keyboard Manager and can't be remapped normally.

## Solution

This tool monitors HP's WMI events (`hpqBEvnt` in the `root\wmi` namespace) and allows you to bind custom actions to these special keys.

## Features

- ✅ Detects HP special function keys via WMI events
- ✅ Fully configurable via JSON
- ✅ Runs automatically at logon via scheduled task
- ✅ Supports multiple hotkeys
- ✅ Optional logging for debugging
- ✅ Easy install/uninstall scripts

## Requirements

- Windows 10/11
- HP laptop with special function keys
- PowerShell 5.1 or later
- Administrator privileges (for installation only)

## Installation

1. Clone this repository or download as ZIP
2. Right-click `Install.ps1` and select "Run with PowerShell"
3. Accept the UAC prompt (required for creating scheduled task)
4. Edit `config.json` to customize your hotkey actions

## Configuration

Edit `config.json` to define your hotkeys:

```
{
"EnableLogging": false,
"LogFile": "C:\Logs\HP-HotkeyHandler.log",
"Hotkeys": [
{
"Name": "F11 Key",
"EventID": 29,
"EventData": 8616,
"ActionType": "LaunchApplication",
"ActionValue": "notepad.exe"
}
]
}
```


### Action Types

- **LaunchApplication**: Start an executable
```
"ActionType": "LaunchApplication",
"ActionValue": "C:\Program Files\MyApp\app.exe"
```


- **RunCommand**: Execute a PowerShell command
```
"ActionType": "RunCommand",
"ActionValue": "Get-Process | Out-GridView"
```


- **OpenURL**: Open a website
```
"ActionType": "OpenURL",
"ActionValue": "https://github.com"
```


## Finding Your Key's EventID

If you want to map a different HP special key:

1. Run this PowerShell command:
`Register-WmiEvent -Namespace "root\wmi" -Query "SELECT * FROM hpqBEvnt" -SourceIdentifier "HPQButton"`


2. Press your special key

3. Check the event:
`Get-Event -SourceIdentifier "HPQButton" | ForEach-Object {
$_.SourceEventArgs.NewEvent | Format-List EventID, EventData
}`

4. Add the EventID and EventData values to your `config.json`

## Uninstallation

Right-click `Uninstall.ps1` and select "Run with PowerShell"

## Troubleshooting

### Handler not starting
- Check Task Scheduler for errors: `taskschd.msc`
- Look for task named "HP-WMI-Hotkey-Handler"

### Key not responding
- Enable logging and check the log file: `C:\Logs\HP-HotkeyHandler.log`
- Verify EventID and EventData match your key (see "Finding Your Key's EventID")

### Permission errors
- Ensure you ran `Install.ps1` as Administrator

## Contributing

Contributions welcome! Please open an issue or PR.
