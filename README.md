# HP Button Remap (Lightweight Rewrite)

Lightweight tray application for remapping HP laptop special function keys (MyHP/HP System Event Utility/HP Programmable Key). Tested on an HP OmniBook, YMMV on other models. This is a vibe coded app, use at your own risk.

## Features

- remap to key/shortcut
- send text
- run program
- run command
- open URL

## Requirements

- Windows 10/11
- HP laptop exposing WMI events in `root\wmi` (`hpqBEvnt`)
- .NET 8 Desktop Runtime installed

## Install

1. Download and unzip the lightweight package.
2. Run `HPButtonRemap.exe`.

On first run, the app self-installs to `%LOCALAPPDATA%\HPButtonRemap\` and relaunches from there.

## Uninstall

Use tray menu -> **Uninstall...**

## Configuration

Edit `%LOCALAPPDATA%\HPButtonRemap\config.json` (or use tray menu -> **Open Configuration**) and then **Reload Configuration**.

Default example (launch a command in a new window):

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "F11 Key - Run a command",
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
- `RunAtStartup` (root config setting)

## Build

```powershell
dotnet publish .\HPButtonRemap\HPButtonRemap.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true
```

`wt.exe` is only an example command above; the utility does not have Windows Terminal-specific behavior.

## License

MIT (see `LICENSE`)
