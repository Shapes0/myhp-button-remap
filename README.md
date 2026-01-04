# HP Button Remap

Lightweight tray application for remapping HP laptop special function keys.

## Overview

This is a **Windows tray application** that monitors HP WMI events and allows you to configure custom actions when special HP laptop buttons are pressed. The application runs in your user session with a system tray icon.

## Key Features

- ✅ **Simple tray application** - Runs in your system tray
- ✅ **No administrator required** - Installs to your user account
- ✅ **GUI Configurator** - Easy-to-use interface for configuration
- ✅ **Multiple action types:**
  - Launch Application (with parameters)
  - Open Website
  - Send Keyboard Shortcuts
- ✅ **Auto-start** - Automatically starts when you log in
- ✅ **Lightweight** - Minimal resource usage

## Problem

HP laptops have special function keys (like F11 with custom icons) that trigger WMI events rather than standard keyboard input. These keys are invisible to tools like PowerToys and AutoHotkey.

## Solution

This lightweight tray application monitors HP's WMI events (`hpqBEvnt`) and executes configured actions. Configure it easily through the GUI or by right-clicking the tray icon.

## Requirements

- Windows 10/11
- HP laptop with special function keys
- **.NET 8.0 Runtime** ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0/runtime) - most modern Windows systems already have this)

## Installation

**Simple One-Click Installation:**

1. **Download** `HPButtonRemap-Installer.exe` (~66MB) from the [Releases page](https://github.com/Shapes0/myhp-button-remap/releases)
2. **Run** the installer
3. **Click "Install"**
4. **Done!** The tray icon should appear in your system tray

The installer will:
- Install the application to `%LOCALAPPDATA%\HPButtonRemap\` (~3MB)
- Add a shortcut to your Startup folder (auto-start on login)
- Add "HP Button Remap Configurator" to your Start Menu
- Start the application immediately

## Configuration

### Using the GUI Configurator

1. **Right-click** the tray icon and select **"Open Configurator"**
   - OR open Start Menu and search for "HP Button Remap Configurator"
2. **Add/Edit/Delete** button actions using the GUI
3. **Save** your configuration
4. **Right-click** tray icon and select **"Reload Configuration"** to apply changes

### Using the Tray Menu

Right-click the tray icon for quick access to:
- **Open Configuration** - Edit config.json directly
- **Open Configurator** - Launch the GUI configurator
- **Reload Configuration** - Apply changes after editing
- **About** - View application info
- **Exit** - Close the application

### Action Types

#### Launch Application
- Browse to select any `.exe` file
- Optionally add command-line arguments
- Example: Launch Chrome with a specific URL

#### Open Website
- Enter any website URL
- Opens in your default browser
- Example: `https://www.google.com`

#### Send Keyboard Shortcut
- Specify key combinations with `+` separator
- Supported keys: Ctrl, Shift, Alt, Win, F1-F12, A-Z, 0-9, special keys
- Examples:
  - `Ctrl+Shift+T` - Reopen closed tab
  - `Win+D` - Show desktop
  - `Ctrl+Alt+Delete` - Security screen

## Uninstallation

**Method 1: Via Tray Icon** (Easiest)
1. **Right-click** the tray icon
2. Select **"Uninstall..."**
3. **Confirm** the uninstallation
4. Your configuration will be backed up to your Desktop

**Method 2: Via Installer**
1. Run `HPButtonRemap-Installer.exe --uninstall`
2. Follow the uninstallation wizard

This will:
- Stop the application
- Remove the Startup shortcut
- Remove the Start Menu shortcut
- Remove the installation directory
- Optionally backup your config file to Desktop

## Troubleshooting

### Tray icon not visible
- Check if the application is running in Task Manager
- Look in the hidden icons area of the system tray
- Restart the application from the Start Menu

### Button not responding
- Right-click tray icon and select "Reload Configuration"
- Verify Event ID is `29` and Event Data is `8616` in your config
- Make sure your action details are correct

### Configurator won't open
- Make sure you have the latest release
- Check that `HPButtonRemapConfig.exe` exists in `%LOCALAPPDATA%\HPButtonRemap\`
- Try editing the config.json file directly instead

## Finding Your Button's Event ID

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

6. Enter these values in the configurator GUI or config.json

## Technical Details

- **Architecture**: Windows Forms tray application
- **GUI**: WPF Application (.NET 8.0)
- **Platform**: Windows (x64)
- **Distribution**: Self-contained single-file executables
- **Dependencies**: 
  - System.Management (WMI event monitoring)
  - Newtonsoft.Json (JSON configuration)
- **Installation Location**: `%LOCALAPPDATA%\HPButtonRemap\`
- **Configuration File**: `config.json` in installation directory

## Building from Source

If you want to build from source:

```powershell
# Clone the repository
git clone https://github.com/Shapes0/myhp-button-remap.git
cd myhp-button-remap

# Build the tray application
cd HPButtonRemap
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true

# Build the configurator
cd ../HPButtonRemapConfig
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true

# Build the installer (optional - embeds the above two)
cd ../Installer
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true

# The installer will be in Installer/bin/Release/net8.0-windows/win-x64/publish/
```

**Requirements for building:**
- .NET 8.0 SDK
- Windows 10/11
- Visual Studio or VS Code (optional, for IDE support)

**Alternative: PowerShell Scripts**

For manual installation without the GUI installer:
```powershell
# Build the components first, then run:
.\Install.ps1
```

## Configuration File Format

The configuration is stored in JSON format:

```json
{
  "ShowStartupNotification": true,
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

**Settings:**
- `ShowStartupNotification` - (true/false) Show balloon notification when tray app starts with action count. Can be disabled in the configurator GUI if you find it annoying.

See `CONFIG-EXAMPLES.md` for more examples.

## Advantages

### Simple & Lightweight
- ✅ No Windows Service complexity
- ✅ No Session 0 isolation issues
- ✅ Runs directly in your user session
- ✅ Visible in Task Manager

### Easy to Use
- ✅ System tray integration
- ✅ GUI configurator
- ✅ No administrator rights needed (after installation)
- ✅ Standard Windows application behavior

### Reliable
- ✅ Auto-starts on login
- ✅ Applications launch in correct session
- ✅ Easy to troubleshoot

## Contributing

Contributions welcome! Please open an issue or PR.

## License

MIT License - See LICENSE file for details
