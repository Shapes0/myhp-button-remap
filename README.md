# HP Button Remap (Lightweight Rewrite)

Tiny tray utility for HP special buttons (`hpqBEvnt`) with PowerToys-style output actions:

- remap to key/shortcut
- send text
- run program
- run command
- open URL

This rewrite removes the heavy bundled installer flow and uses a lightweight script install/uninstall approach.

## Why this rewrite

The old packaging path produced large installers and inconsistent launch behavior for some commands (notably `wt.exe` in certain environments).  
This version focuses on:

- small framework-dependent single-file app distribution
- robust command/program launching with fallback handling
- JSON-first config with no mandatory GUI config tool

## Requirements

- Windows 10/11
- HP laptop exposing WMI events in `root\wmi` (`hpqBEvnt`)
- .NET 8 Desktop Runtime installed

## Install

From a PowerShell prompt in this repo/release folder:

```powershell
.\Install.ps1
```

What it does:

- installs to `%LOCALAPPDATA%\HPButtonRemap\`
- registers startup shortcut (`HP Button Remap.lnk`)
- starts the tray app

## Uninstall

```powershell
.\Uninstall.ps1
```

or run `%LOCALAPPDATA%\HPButtonRemap\Uninstall.ps1`.

## Configuration

Edit `%LOCALAPPDATA%\HPButtonRemap\config.json` (or use tray menu -> **Open Configuration**) and then **Reload Configuration**.

Default example (launch a new Windows Terminal):

```json
{
  "ShowStartupNotification": true,
  "ButtonActions": [
    {
      "Name": "F11 Key - Open Windows Terminal",
      "EventID": 29,
      "EventData": 8616,
      "Type": "RunCommand",
      "Command": "wt.exe",
      "CreateNewWindow": true
    }
  ]
}
```

## Action types

### `RemapKey`
Send a key or shortcut.

```json
{
  "Type": "RemapKey",
  "RemapTo": "Ctrl+Shift+T"
}
```

(`SendKeys` + `KeyCombo` is still supported for backward compatibility.)

### `SendText`
Send Unicode text to the focused window.

```json
{
  "Type": "SendText",
  "Text": "hello from my HP key"
}
```

### `LaunchApp`
Run an executable with optional arguments.

```json
{
  "Type": "LaunchApp",
  "ProgramPath": "notepad.exe",
  "ProgramArguments": ""
}
```

Legacy `LaunchPath`/`LaunchArguments` still works.

### `RunCommand`
Run a command through `cmd.exe`. Useful for aliases/shell commands.

```json
{
  "Type": "RunCommand",
  "Command": "wt.exe"
}
```

### `OpenWebsite`
Open URL in default browser.

```json
{
  "Type": "OpenWebsite",
  "Url": "https://example.com"
}
```

Legacy `WebsiteUrl` still works.

## Advanced per-action options

- `DelayMs` (default `0`)
- `WorkingDirectory`
- `CreateNewWindow` (for `RunCommand`, default `true`)
- `UseShellExecute` (for `LaunchApp`, default `true`)
- `WaitForExit` (default `false`)

## Build

```powershell
dotnet publish .\HPButtonRemap\HPButtonRemap.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true
```

## Notes on Windows Terminal (`wt.exe`)

The launcher includes fallback logic for common `wt.exe` alias/path issues by trying:

1. normal launch
2. `%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe`
3. `explorer.exe shell:AppsFolder\Microsoft.WindowsTerminal_8wekyb3d8bbwe!App`

## License

MIT (see `LICENSE`)
