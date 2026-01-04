# myhp-button-remap

Native Windows application for remapping HP laptop special function keys.

## Overview

This is a **native Windows C# application** that monitors HP WMI events and allows you to configure custom actions when special HP laptop buttons are pressed. It runs efficiently in the background without the overhead of PowerShell scripts.

## Problem

HP laptops have special function keys (like F11 with custom icons) that trigger WMI events rather than standard keyboard input. These keys are invisible to tools like PowerToys and AutoHotkey, and can't be remapped normally.

## Solution

This native Windows application monitors HP's WMI events (`hpqBEvnt` in the `root\wmi` namespace) and executes configurable actions when these special keys are pressed.

## Features

- ✅ Native Windows executable (no PowerShell overhead)
- ✅ Detects HP special function keys via WMI events
- ✅ Multiple action types:
  - **Launch Application** - Start any program with optional command-line arguments
  - **Open Website** - Open URLs in your default browser
  - **Send Key Combo** - Simulate keyboard shortcuts (e.g., Ctrl+Shift+T)
- ✅ JSON-based configuration
- ✅ Runs automatically at logon via scheduled task
- ✅ Supports multiple button mappings
- ✅ Lightweight and efficient
- ✅ Easy install/uninstall scripts

## Requirements

- Windows 10/11
- HP laptop with special function keys
- .NET 8.0 Runtime (only needed if building from source)
- Administrator privileges (for installation only)

## Quick Start

### Option 1: Using Pre-built Binary (Recommended)

1. **Download the latest release** from the [Releases page](https://github.com/Shapes0/myhp-button-remap/releases)
2. **Extract the ZIP file** to a folder of your choice
3. **Edit** `config.json` to set your desired actions (see examples below)
4. **Right-click** `Install.ps1` and select **"Run with PowerShell"**
5. Accept the UAC prompt
6. The application will start automatically at next logon, or run: `Start-ScheduledTask -TaskName "HP-Button-Remap"`

### Option 2: Building from Source

If you want to build from source instead of using the prebuilt binary:

```powershell
# Clone the repository
git clone https://github.com/Shapes0/myhp-button-remap.git
cd myhp-button-remap

# Build the project
cd HPButtonRemap
dotnet build --configuration Release

# Return to root and install
cd ..
# Right-click Install.ps1 and select "Run with PowerShell"
```

**Requirements for building from source:**
- .NET 8.0 SDK
- Windows 10/11

## Configuration

Edit `config.json` in the root directory to define your button action:

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Launch Notepad",
      "EventID": 29,
      "EventData": 8616,
      "Type": "LaunchApp",
      "LaunchPath": "notepad.exe",
      "LaunchArguments": ""
    }
  ]
}
```

### Configuration Fields

- **Name**: Descriptive name for the action (for your reference)
- **EventID**: The WMI event ID for your key (29 for the HP F11 button)
- **EventData**: Additional event data to filter on (8616 for the HP F11 button)
- **Type**: Action type - one of: `LaunchApp`, `OpenWebsite`, or `SendKeys`
- **LaunchPath**: Path to executable (for `LaunchApp` type)
- **LaunchArguments**: Command-line arguments (for `LaunchApp` type, optional)
- **WebsiteUrl**: URL to open (for `OpenWebsite` type)
- **KeyCombo**: Keyboard shortcut to send (for `SendKeys` type, e.g., "Ctrl+Shift+T")

### Action Type Examples

#### Launch an Application

```json
{
  "Name": "Open Calculator",
  "EventID": 29,
  "EventData": 8616,
  "Type": "LaunchApp",
  "LaunchPath": "calc.exe",
  "LaunchArguments": ""
}
```

#### Launch Application with Arguments

```json
{
  "Name": "Open Notepad with File",
  "EventID": 29,
  "EventData": 8616,
  "Type": "LaunchApp",
  "LaunchPath": "C:\\Windows\\System32\\notepad.exe",
  "LaunchArguments": "C:\\Users\\YourName\\Documents\\notes.txt"
}
```

#### Open a Website

```json
{
  "Name": "Open Google",
  "EventID": 29,
  "EventData": 8616,
  "Type": "OpenWebsite",
  "WebsiteUrl": "https://www.google.com"
}
```

#### Send Keyboard Shortcut

```json
{
  "Name": "Reopen Closed Tab",
  "EventID": 29,
  "EventData": 8616,
  "Type": "SendKeys",
  "KeyCombo": "Ctrl+Shift+T"
}
```

**Supported Keys for KeyCombo:**
- Modifiers: `Ctrl`, `Shift`, `Alt`, `Win`
- Function Keys: `F1` through `F12`
- Special Keys: `Esc`, `Tab`, `Enter`, `Space`, `Backspace`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`, `Up`, `Down`, `Left`, `Right`
- Letters: `A` through `Z`
- Numbers: `0` through `9`

**Examples:**
- `Ctrl+C` - Copy
- `Ctrl+V` - Paste
- `Alt+Tab` - Switch windows
- `Win+D` - Show desktop
- `Ctrl+Shift+Esc` - Task Manager

## Finding Your Key's EventID

If you want to map a different HP special key:

1. Open PowerShell and run:
   ```powershell
   Register-WmiEvent -Namespace "root\wmi" -Query "SELECT * FROM hpqBEvnt" -SourceIdentifier "HPTest"
   ```

2. Press your special key

3. Check the event details:
   ```powershell
   Get-Event -SourceIdentifier "HPTest" | ForEach-Object {
       $_.SourceEventArgs.NewEvent | Format-List EventID, EventData
   }
   ```

4. Note the `EventID` and `EventData` values

5. Clean up:
   ```powershell
   Unregister-Event -SourceIdentifier "HPTest"
   Remove-Event *
   ```

6. Add these values to your `config.json`

## Multiple Button Actions

If your HP laptop has multiple special buttons with different EventIDs, you can configure them all:

```json
{
  "ButtonActions": [
    {
      "Name": "F11 - Open Browser",
      "EventID": 29,
      "EventData": 8616,
      "Type": "OpenWebsite",
      "WebsiteUrl": "https://www.google.com"
    },
    {
      "Name": "Another Button",
      "EventID": 15,
      "EventData": 4321,
      "Type": "LaunchApp",
      "LaunchPath": "calc.exe"
    }
  ]
}
```

Note: Most HP laptops only have one special button (EventID 29, EventData 8616). Use the discovery method below if you want to find additional buttons.

## Installation

Right-click `Install.ps1` and select **"Run with PowerShell"**

The installer will:
- Build the application if needed
- Create a scheduled task that runs at logon
- Configure the task to restart automatically if it crashes

## Uninstallation

Right-click `Uninstall.ps1` and select **"Run with PowerShell"**

This will:
- Stop any running instances
- Remove the scheduled task

## Troubleshooting

### Application not starting
- Open Task Scheduler (`taskschd.msc`)
- Find "HP-Button-Remap" task
- Check the "History" tab for errors
- Try running manually: `Start-ScheduledTask -TaskName "HP-Button-Remap"`

### Key not responding
- Verify your EventID and EventData are correct (use the discovery method above)
- Check the configuration file syntax
- Make sure the action details are correct (e.g., valid file path for LaunchApp)

### Build errors
- Ensure .NET 8.0 SDK is installed: `dotnet --version`
- Run `dotnet restore` in the HPButtonRemap directory
- Check for any error messages during build

### Permission errors
- Ensure you ran `Install.ps1` as Administrator
- The scheduled task needs to run with your user account
- For SendKeys actions, the application needs to be in the foreground context

## How It Works

1. The native C# application uses the `System.Management` library to subscribe to HP's WMI events
2. When you press a special key, HP's ACPI driver fires a WMI event with specific EventID and EventData
3. The application catches this event and executes your configured action
4. The scheduled task keeps the application running in the background

## Technical Details

- **Language**: C# / .NET 8.0
- **Platform**: Windows (net8.0-windows)
- **Dependencies**: 
  - System.Management 10.0.1 (WMI event monitoring)
  - Newtonsoft.Json 13.0.4 (JSON configuration parsing)
- **Architecture**: Single-threaded event-driven design
- **Distribution**: Prebuilt binaries available via GitHub Releases, automated by GitHub Actions

## Releases

Prebuilt binaries are automatically compiled and packaged using GitHub Actions whenever a new version tag is created. 

**For Users:**
- Download the latest release ZIP from the [Releases page](https://github.com/Shapes0/myhp-button-remap/releases)
- The ZIP contains everything you need: the executable, config files, and install scripts
- No need to install .NET SDK or build from source

**For Maintainers:**
To create a new release:
1. Tag the commit with a version number: `git tag v1.0.0`
2. Push the tag: `git push origin v1.0.0`
3. GitHub Actions will automatically build and create a release with the compiled binary

## Differences from Previous PowerShell Version

The original PowerShell-based implementation has been replaced with this native application for the following benefits:

- **Better Performance**: Native compiled code vs. interpreted PowerShell
- **Lower Resource Usage**: No PowerShell host overhead
- **More Reliable**: Proper error handling and restart capabilities
- **Extended Functionality**: Support for key combos and more action types
- **Cleaner Implementation**: Strongly-typed configuration and proper separation of concerns

## Contributing

Contributions welcome! Please open an issue or PR.

## License

MIT License - See LICENSE file for details
