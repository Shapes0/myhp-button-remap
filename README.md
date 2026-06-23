# HP Button Remap (Single-Button Config)

Lightweight tray app for remapping HP special function button events from `hpqBEvnt`.

## Requirements

- Windows 10/11
- HP device exposing `root\wmi` `hpqBEvnt` events
- .NET 8 Desktop Runtime

## Install

1. Download `HPButtonRemap.exe`
2. Run it once

It self-installs to `%LOCALAPPDATA%\HPButtonRemap\`.

## Uninstall

Tray icon -> **Uninstall...**

## Configuration

Edit `%LOCALAPPDATA%\HPButtonRemap\config.json` and click tray menu -> **Reload Configuration**.

### Config shape

- `EventID` and `EventData` are root-level (single button model)
- one `Action` object (not an array)

Built-in default `config.json` is focused on `RunCommand`:

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "RunCommand",
    "Command": "%localappdata%\\Microsoft\\WindowsApps\\wt.exe",
    "CreateNewWindow": true
  }
}
```

## Full example configs

### 1) RunCommand

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "RunCommand",
    "Command": "%localappdata%\\Microsoft\\WindowsApps\\wt.exe",
    "CreateNewWindow": true,
    "WorkingDirectory": "%userprofile%",
    "DelayMs": 0
  }
}
```

### 2) RemapKey

```json
{
  "ShowStartupNotification": false,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "RemapKey",
    "RemapKey": "Ctrl+Shift+T",
    "DelayMs": 0
  }
}
```

### 3) SendText

```json
{
  "ShowStartupNotification": false,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "SendText",
    "SendText": "Best regards, Alex",
    "DelayMs": 0
  }
}
```

### 4) OpenWebsite

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "OpenWebsite",
    "OpenWebsite": "https://learn.microsoft.com/windows/terminal/",
    "DelayMs": 0
  }
}
```

## Build locally

```powershell
dotnet publish .\HPButtonRemap\HPButtonRemap.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  --output .\publish\app
```

## License

MIT (see `LICENSE`)
