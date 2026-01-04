using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HPButtonRemap;

/// <summary>
/// Executes actions based on button configuration
/// </summary>
public class ActionExecutor
{
    // Import necessary Windows API functions for sending keyboard input
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// Execute an action based on its configuration
    /// </summary>
    public void ExecuteAction(ButtonAction action, Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            logger.LogInformation("Executing action: {ActionName} (Type: {ActionType})", action.Name, action.Type);
            
            switch (action.Type)
            {
                case ActionType.LaunchApp:
                    LaunchApplication(action, logger);
                    break;
                case ActionType.OpenWebsite:
                    OpenWebsite(action, logger);
                    break;
                case ActionType.SendKeys:
                    SendKeyCombo(action, logger);
                    break;
                default:
                    logger.LogError("Unknown action type: {ActionType}", action.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute action '{ActionName}'", action.Name);
        }
    }

    /// <summary>
    /// Launch an application with optional arguments
    /// </summary>
    private void LaunchApplication(ButtonAction action, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (string.IsNullOrEmpty(action.LaunchPath))
        {
            logger.LogError("LaunchPath is not specified");
            return;
        }

        // If running as a service (Session 0), use special launcher to launch in user session
        if (UserSessionLauncher.IsRunningAsService())
        {
            logger.LogInformation("Running as service, launching in user session...");
            if (UserSessionLauncher.LaunchProcessInUserSession(action.LaunchPath, action.LaunchArguments ?? string.Empty, out string error))
            {
                logger.LogInformation("Launched in user session: {LaunchPath} {LaunchArguments}", action.LaunchPath, action.LaunchArguments);
            }
            else
            {
                logger.LogError("Failed to launch in user session: {Error}", error);
            }
        }
        else
        {
            // Normal launch when running in user context
            var startInfo = new ProcessStartInfo
            {
                FileName = action.LaunchPath,
                Arguments = action.LaunchArguments ?? string.Empty,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            logger.LogInformation("Launched: {LaunchPath} {LaunchArguments}", action.LaunchPath, action.LaunchArguments);
        }
    }

    /// <summary>
    /// Open a website in the default browser
    /// </summary>
    private void OpenWebsite(ButtonAction action, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (string.IsNullOrEmpty(action.WebsiteUrl))
        {
            logger.LogError("WebsiteUrl is not specified");
            return;
        }

        // If running as a service (Session 0), use special launcher to launch in user session
        if (UserSessionLauncher.IsRunningAsService())
        {
            logger.LogInformation("Running as service, launching browser in user session...");
            // Use cmd /c start to open URL with default browser
            if (UserSessionLauncher.LaunchProcessInUserSession("cmd.exe", $"/c start \"\" \"{action.WebsiteUrl}\"", out string error))
            {
                logger.LogInformation("Opened website in user session: {WebsiteUrl}", action.WebsiteUrl);
            }
            else
            {
                logger.LogError("Failed to open website in user session: {Error}", error);
            }
        }
        else
        {
            // Normal launch when running in user context
            var startInfo = new ProcessStartInfo
            {
                FileName = action.WebsiteUrl,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            logger.LogInformation("Opened website: {WebsiteUrl}", action.WebsiteUrl);
        }
    }

    /// <summary>
    /// Send keyboard shortcut (e.g., "Ctrl+Shift+T")
    /// </summary>
    private void SendKeyCombo(ButtonAction action, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (string.IsNullOrEmpty(action.KeyCombo))
        {
            logger.LogError("KeyCombo is not specified");
            return;
        }

        var keys = action.KeyCombo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var virtualKeys = new List<byte>();

        foreach (var key in keys)
        {
            if (TryGetVirtualKeyCode(key, out byte vk))
            {
                virtualKeys.Add(vk);
            }
            else
            {
                logger.LogError("Unknown key: {Key}", key);
                return;
            }
        }

        // Press all keys down
        foreach (var vk in virtualKeys)
        {
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
        }

        // Release all keys in reverse order
        for (int i = virtualKeys.Count - 1; i >= 0; i--)
        {
            keybd_event(virtualKeys[i], 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        logger.LogInformation("Sent key combo: {KeyCombo}", action.KeyCombo);
    }

    /// <summary>
    /// Map key name to Windows virtual key code
    /// </summary>
    private bool TryGetVirtualKeyCode(string keyName, out byte virtualKey)
    {
        virtualKey = keyName.ToUpper() switch
        {
            // Modifier keys
            "CTRL" or "CONTROL" => 0x11, // VK_CONTROL
            "SHIFT" => 0x10,              // VK_SHIFT
            "ALT" => 0x12,                // VK_MENU
            "WIN" or "WINDOWS" => 0x5B,   // VK_LWIN
            
            // Function keys
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            
            // Special keys
            "ESC" or "ESCAPE" => 0x1B,
            "TAB" => 0x09,
            "ENTER" or "RETURN" => 0x0D,
            "SPACE" => 0x20,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            
            // Letters (A-Z)
            string s when s.Length == 1 && char.IsLetter(s[0]) => (byte)s[0],
            
            // Numbers (0-9)
            string s when s.Length == 1 && char.IsDigit(s[0]) => (byte)s[0],
            
            _ => 0x00
        };

        return virtualKey != 0x00;
    }
}
