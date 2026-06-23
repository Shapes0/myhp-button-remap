# HP Button Remap Configuration Examples

All examples assume your HP key produces:

- `EventID`: `29`
- `EventData`: `8616`

Use your own values if they differ.

## Launch Windows Terminal (recommended for your use case)

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "Open Windows Terminal",
      "EventID": 29,
      "EventData": 8616,
      "Type": "RunCommand",
      "Command": "wt.exe",
      "CreateNewWindow": true
    }
  ]
}
```

## Remap to another shortcut

```json
{
  "ShowStartupNotification": false,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "Reopen browser tab",
      "EventID": 29,
      "EventData": 8616,
      "Type": "RemapKey",
      "RemapTo": "Ctrl+Shift+T"
    }
  ]
}
```

## Send a string of text

```json
{
  "ShowStartupNotification": false,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "Insert signature",
      "EventID": 29,
      "EventData": 8616,
      "Type": "SendText",
      "Text": "Best regards, Alex"
    }
  ]
}
```

## Run a program directly

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "Launch Notepad",
      "EventID": 29,
      "EventData": 8616,
      "Type": "LaunchApp",
      "ProgramPath": "notepad.exe",
      "ProgramArguments": ""
    }
  ]
}
```

## Open a URL

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "Open docs",
      "EventID": 29,
      "EventData": 8616,
      "Type": "OpenWebsite",
      "Url": "https://learn.microsoft.com/windows/terminal/"
    }
  ]
}
```

## Multiple actions for different HP events

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "ButtonActions": [
    {
      "Name": "Terminal",
      "EventID": 29,
      "EventData": 8616,
      "Type": "RunCommand",
      "Command": "wt.exe"
    },
    {
      "Name": "Browser",
      "EventID": 15,
      "EventData": 4321,
      "Type": "OpenWebsite",
      "Url": "https://example.com"
    }
  ]
}
```
