# HP Button Remap

Windows Service with GUI Configurator for remapping HP laptop special function keys.

## Overview

This is a **Windows Service** with a **GUI configurator** that monitors HP WMI events and allows you to configure custom actions when special HP laptop buttons are pressed. The service runs as LocalSystem but uses Windows APIs to launch applications in your user session, providing a seamless experience without credential configuration.

## Key Features

- ✅ **Windows Service** - Runs automatically, no credential setup needed
- ✅ **Launches in your session** - Uses Windows APIs to launch apps in the active user session
- ✅ **GUI Configurator** - Easy-to-use interface accessible from Start Menu
- ✅ **No manual config editing** - Configure everything through the GUI
- ✅ **Multiple action types:**
  - Launch Application (with parameters)
  - Open Website
  - Send Keyboard Shortcuts
- ✅ **Simple installation** - Run one PowerShell script as admin
- ✅ **Reliable** - Service auto-starts and recovers from crashes

## Problem

HP laptops have special function keys (like F11 with custom icons) that trigger WMI events rather than standard keyboard input. These keys are invisible to tools like PowerToys and AutoHotkey.

## Solution

This Windows Service monitors HP's WMI events (`hpqBEvnt`) and executes configured actions. Configure it easily through the GUI app in your Start Menu.

## Requirements

- Windows 10/11
- HP laptop with special function keys
- Administrator privileges (for initial installation only)
- No .NET runtime needed (self-contained executable)

## Installation

1. **Download** the latest release from the [Releases page](https://github.com/Shapes0/myhp-button-remap/releases)
2. **Extract** the ZIP file to a temporary folder
3. **Right-click** `Install.ps1` and select **"Run with PowerShell"**
4. **Accept** the UAC prompt (administrator required)
5. **Done!** The service is now running

The installer will:
- Install the service to `C:\Program Files\HPButtonRemap\`
- Start the service as LocalSystem (no credential configuration needed)
- Add "HP Button Remap Configurator" to your Start Menu

**How it works:** The service runs in Session 0 but uses Windows APIs (`CreateProcessAsUser`) to launch applications in the active user's session. This means no credential configuration is needed, and applications launch properly in your desktop.

## Configuration

### Using the GUI Configurator

1. **Open Start Menu** and search for "HP Button Remap Configurator"
2. **Click** the configurator app
3. **Add/Edit/Delete** button actions using the GUI
4. **Save** your configuration
5. **Restart Service** button to apply changes

### GUI Features

- **Add new actions** - Click "Add" to create a new button action
- **Edit existing actions** - Select an action and click "Edit"
- **Delete actions** - Select an action and click "Delete"
- **Action Types:**
  - **Launch App** - Browse for an executable, add command-line arguments
  - **Open Website** - Enter any URL
  - **Send Keys** - Specify key combinations (e.g., Ctrl+Shift+T)

### Manual Configuration (Advanced)

The configuration file is stored at:
```
C:\Program Files\HPButtonRemap\config.json
```

You can edit it manually if needed. See `CONFIG-EXAMPLES.md` for examples.

## Action Types

### Launch Application
- Browse to select any `.exe` file
- Optionally add command-line arguments
- Example: Launch Chrome with a specific URL

### Open Website
- Enter any website URL
- Opens in your default browser
- Example: `https://www.google.com`

### Send Keyboard Shortcut
- Specify key combinations with `+` separator
- Supported keys: Ctrl, Shift, Alt, Win, F1-F12, A-Z, 0-9, special keys
- Examples:
  - `Ctrl+Shift+T` - Reopen closed tab
  - `Win+D` - Show desktop
  - `Ctrl+Alt+Delete` - Security screen

## Uninstallation

1. **Right-click** `Uninstall-Service.ps1` and select **"Run with PowerShell"**
2. **Accept** the UAC prompt
3. **Choose** whether to keep your configuration file

This will:
- Stop and remove the Windows Service
- Remove the Start Menu shortcut
- Remove the installation directory
- Optionally backup your config file

## Troubleshooting

### Service not starting
- Open Services (`services.msc`)
- Find "HP Button Remap Service"
- Check the startup type is "Automatic"
- Try starting it manually
- Check Windows Event Log for errors

### Button not responding
- Open the configurator
- Verify Event ID is `29` and Event Data is `8616`
- Make sure your action details are correct
- Save and restart the service

### Configurator won't open
- Make sure you have the latest release
- Check that `HPButtonRemapConfig.exe` exists in Program Files
- Run as administrator if needed

### Configuration not applying
- After making changes in the GUI, click "Restart Service"
- Check that config.json was updated (modify date should change)
- Restart the service manually from Services if needed

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

6. Enter these values in the configurator GUI

## Technical Details

- **Service**: Windows Service (Background Service Worker)
- **GUI**: WPF Application (.NET 8.0)
- **Platform**: Windows (x64)
- **Distribution**: Self-contained single-file executables
- **Dependencies**: 
  - System.Management (WMI event monitoring)
  - Newtonsoft.Json (JSON configuration)
- **Installation Location**: `C:\Program Files\HPButtonRemap\`
- **Configuration File**: `config.json` in installation directory

## Building from Source

If you want to build from source:

```powershell
# Clone the repository
git clone https://github.com/Shapes0/myhp-button-remap.git
cd myhp-button-remap

# Build the service
cd HPButtonRemap
dotnet build --configuration Release

# Build the configurator
cd ../HPButtonRemapConfig
dotnet build --configuration Release

# Return to root
cd ..

# Run Install-Service.ps1 as administrator
```

**Requirements for building:**
- .NET 8.0 SDK
- Windows 10/11
- Visual Studio or VS Code (optional, for IDE support)

## Configuration File Format

The configuration is stored in JSON format:

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

See `CONFIG-EXAMPLES.md` for more examples.

## Advantages Over Previous Versions

### vs. Scheduled Task
- ✅ Cleaner - runs as a proper Windows Service
- ✅ Better integration with Windows
- ✅ Easier management through Services panel
- ✅ Standard service control (start/stop/restart)

### vs. Startup Folder
- ✅ More reliable - service starts before user login
- ✅ Survives logoff/switch user
- ✅ Proper Windows Service lifecycle management
- ✅ Better error recovery

### vs. Manual Config
- ✅ GUI configurator - no need to edit JSON files
- ✅ Validation - prevents configuration errors
- ✅ User-friendly - accessible from Start Menu
- ✅ Visual interface - easier for non-technical users

## Contributing

Contributions welcome! Please open an issue or PR.

## License

MIT License - See LICENSE file for details
