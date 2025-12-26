# myhp-button-remap

Remap F11 key MyHP/HP System Event Utility button

After some back and forth with Claude I was able to figure this out. This is a rudimentary script, use at your own risk. Tested on an HP OmniBook, YMMV on other models.

Custom handler for HP laptop special function keys that don't generate standard keyboard scancodes.

## Problem

HP laptops have special function keys (like F11 with custom icons) that trigger WMI events rather than standard keyboard input. These keys are invisible to tools like PowerToys and AutoHotkey, and can't be remapped normally.

## Solution

This tool monitors HP's WMI events (`hpqBEvnt` in the `root\wmi` namespace) and launches applications or commands when these special keys are pressed.

## Features

- ✅ Detects HP special function keys via WMI events
- ✅ Simple JSON configuration
- ✅ Runs automatically at logon via scheduled task
- ✅ Supports multiple hotkeys
- ✅ Lightweight and efficient
- ✅ Easy install/uninstall scripts

## Requirements

- Windows 10/11
- HP laptop with special function keys
- PowerShell 5.1 or later
- Administrator privileges (for installation only)

## Quick Start

1. **Clone or download** this repository
2. **Right-click** `Install.ps1` and select **"Run with PowerShell"**
3. Accept the UAC prompt
4. **Edit** `config.json` to set your desired application
5. **Restart** or run: `Start-ScheduledTask -TaskName "HP-WMI-Hotkey-Handler"`

## Configuration

Edit `config.json` to define your hotkeys:

```
{
"Hotkeys": [
{
"Name": "F11 Key",
"EventID": 29,
"EventData": 8616,
"Command": "notepad.exe"
}
]
}
```

### Configuration Fields

- **Name**: Descriptive name for the hotkey (for your reference)
- **EventID**: The WMI event ID for your key (see below for how to find this)
- **EventData**: Additional event data to filter on (optional but recommended)
- **Command**: The executable or command to run when the key is pressed

### Command Examples

`"Command": "notepad.exe"`

`"Command": "C:\Program Files\MyApp\app.exe"`

`"Command": "powershell.exe -Command Get-Process | Out-GridView"`

## Finding Your Key's EventID

If you want to map a different HP special key:

1. Open PowerShell and run:
`Register-WmiEvent -Namespace "root\wmi" -Query "SELECT * FROM hpqBEvnt" -SourceIdentifier "HPTest"`

2. Press your special key

3. Check the event details:
```
Get-Event -SourceIdentifier "HPTest" | ForEach-Object {
$_.SourceEventArgs.NewEvent | Format-List EventID, EventData
}
```

4. Note the `EventID` and `EventData` values

5. Clean up:
```
Unregister-Event -SourceIdentifier "HPTest"
Remove-Event *
```

7. Add these values to your `config.json`

## Multiple Hotkeys

You can configure multiple keys:

```
{
"Hotkeys": [
{
"Name": "F11 Key",
"EventID": 29,
"EventData": 8616,
"Command": "notepad.exe"
},
{
"Name": "Calculator Key",
"EventID": 15,
"EventData": 4321,
"Command": "calc.exe"
}
]
}
```

## Uninstallation

Right-click `Uninstall.ps1` and select **"Run with PowerShell"**

## Troubleshooting

### Handler not starting
- Open Task Scheduler (`taskschd.msc`)
- Find "HP-WMI-Hotkey-Handler" task
- Check the "History" tab for errors
- Try running manually: `Start-ScheduledTask -TaskName "HP-WMI-Hotkey-Handler"`

### Key not responding
- Verify your EventID and EventData are correct (use the discovery method above)
- Make sure the command path is correct
- Test the command directly in PowerShell first

### Permission errors
- Ensure you ran `Install.ps1` as Administrator
- The scheduled task needs to run with your user account

## How It Works

1. The script uses PowerShell's `Register-WmiEvent` to subscribe to HP's WMI events
2. When you press a special key, HP's ACPI driver fires a WMI event with specific EventID and EventData
3. The script catches this event and executes your configured command
4. The scheduled task keeps the handler running in the background

## Contributing

Contributions welcome! Please open an issue or PR.
