# HP Button Remap Configuration Examples

All examples use the single-button config shape:

- root: `EventID`, `EventData`, `Action`
- no `ButtonActions` array

## RunCommand

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

## RemapKey

```json
{
  "ShowStartupNotification": false,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "RemapKey",
    "RemapKey": "Ctrl+Shift+T"
  }
}
```

## SendText

```json
{
  "ShowStartupNotification": false,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "SendText",
    "SendText": "Best regards, Alex"
  }
}
```

## OpenWebsite

```json
{
  "ShowStartupNotification": true,
  "RunAtStartup": true,
  "EventID": 29,
  "EventData": 8616,
  "Action": {
    "Type": "OpenWebsite",
    "OpenWebsite": "https://example.com"
  }
}
```
