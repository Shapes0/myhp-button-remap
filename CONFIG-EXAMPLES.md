# Configuration Examples for HP Button Remap

This file contains various configuration examples for different action types.

## Basic Configuration (Default)

Launch Notepad when F11 button is pressed:

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

## Launch Application with Arguments

Open a specific file with Notepad:

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Open Notes File",
      "EventID": 29,
      "EventData": 8616,
      "Type": "LaunchApp",
      "LaunchPath": "C:\\Windows\\System32\\notepad.exe",
      "LaunchArguments": "C:\\Users\\YourName\\Documents\\notes.txt"
    }
  ]
}
```

## Open Website

Open your favorite website:

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Open Google",
      "EventID": 29,
      "EventData": 8616,
      "Type": "OpenWebsite",
      "WebsiteUrl": "https://www.google.com"
    }
  ]
}
```

## Send Keyboard Shortcut

Reopen last closed browser tab (Ctrl+Shift+T):

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Reopen Tab",
      "EventID": 29,
      "EventData": 8616,
      "Type": "SendKeys",
      "KeyCombo": "Ctrl+Shift+T"
    }
  ]
}
```

More keyboard shortcut examples:

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Copy",
      "EventID": 29,
      "EventData": 8616,
      "Type": "SendKeys",
      "KeyCombo": "Ctrl+C"
    }
  ]
}
```

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Show Desktop",
      "EventID": 29,
      "EventData": 8616,
      "Type": "SendKeys",
      "KeyCombo": "Win+D"
    }
  ]
}
```

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Key - Task Manager",
      "EventID": 29,
      "EventData": 8616,
      "Type": "SendKeys",
      "KeyCombo": "Ctrl+Shift+Esc"
    }
  ]
}
```

## Multiple Buttons (Advanced)

If you have multiple special buttons on your HP laptop with different EventIDs:

```json
{
  "ButtonActions": [
    {
      "Name": "F11 Button - Browser",
      "EventID": 29,
      "EventData": 8616,
      "Type": "OpenWebsite",
      "WebsiteUrl": "https://www.google.com"
    },
    {
      "Name": "Another Button - Calculator",
      "EventID": 15,
      "EventData": 4321,
      "Type": "LaunchApp",
      "LaunchPath": "calc.exe",
      "LaunchArguments": ""
    }
  ]
}
```

Note: Most HP laptops only have one special button. Use the discovery method in the README to find additional buttons if available.
