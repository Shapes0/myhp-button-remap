using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HPButtonRemap;

/// <summary>
/// Executes actions based on button configuration
/// </summary>
public class ActionExecutor
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private readonly record struct KeyStroke(ushort VirtualKey, bool Extended);

    /// <summary>
    /// Execute an action based on its configuration
    /// </summary>
    public void ExecuteAction(ButtonAction action)
    {
        try
        {
            Debug.WriteLine($"Executing action: {action.Name} (Type: {action.Type})");

            if (action.DelayMs > 0)
            {
                Thread.Sleep(action.DelayMs);
            }

            switch (action.Type)
            {
                case ActionType.RemapKey:
                    SendKeyCombo(action.EffectiveKeyCombo);
                    break;
                case ActionType.SendText:
                    SendText(action.Text);
                    break;
                case ActionType.RunCommand:
                    RunCommand(action);
                    break;
                case ActionType.LaunchApp:
                    LaunchApplication(action);
                    break;
                case ActionType.OpenWebsite:
                    OpenWebsite(action);
                    break;
                default:
                    Debug.WriteLine($"Unknown action type: {action.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to execute action '{action.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Launch an application with optional arguments
    /// </summary>
    private void LaunchApplication(ButtonAction action)
    {
        if (string.IsNullOrWhiteSpace(action.EffectiveProgramPath))
        {
            Debug.WriteLine("Program path is not specified");
            return;
        }

        var fileName = action.EffectiveProgramPath!;
        var arguments = action.EffectiveProgramArguments ?? string.Empty;

        try
        {
            StartProcess(fileName, arguments, action);
            return;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2 || ex.NativeErrorCode == 3)
        {
            // This fallback catches PATH/AppAlias edge-cases (for example wt.exe).
            if (IsWindowsTerminalAlias(fileName) && TryLaunchWindowsTerminal(arguments, action.WorkingDirectory))
            {
                return;
            }

            LaunchViaCmdStart(fileName, arguments, action.WorkingDirectory);
            Debug.WriteLine($"Launched via cmd start fallback: {fileName} {arguments}");
        }
    }

    private void RunCommand(ButtonAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Command))
        {
            Debug.WriteLine("Command is not specified");
            return;
        }

        string arguments = action.CreateNewWindow
            ? $"/d /c start \"\" cmd /d /c {action.Command}"
            : $"/d /c {action.Command}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = arguments,
            UseShellExecute = false
        };

        if (!string.IsNullOrWhiteSpace(action.WorkingDirectory))
        {
            startInfo.WorkingDirectory = action.WorkingDirectory;
        }

        Process.Start(startInfo);
        Debug.WriteLine($"Executed command: {action.Command}");
    }

    /// <summary>
    /// Open a website in the default browser
    /// </summary>
    private void OpenWebsite(ButtonAction action)
    {
        if (string.IsNullOrWhiteSpace(action.EffectiveUrl))
        {
            Debug.WriteLine("URL is not specified");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = action.EffectiveUrl,
            UseShellExecute = true
        };

        Process.Start(startInfo);
        Debug.WriteLine($"Opened URL: {action.EffectiveUrl}");
    }

    /// <summary>
    /// Send keyboard shortcut (e.g., "Ctrl+Shift+T")
    /// </summary>
    private void SendKeyCombo(string? keyCombo)
    {
        if (string.IsNullOrWhiteSpace(keyCombo))
        {
            Debug.WriteLine("Key combo is not specified");
            return;
        }

        var tokens = keyCombo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var strokes = new List<KeyStroke>();

        foreach (var token in tokens)
        {
            if (TryGetVirtualKeyCode(token, out var stroke))
            {
                strokes.Add(stroke);
            }
            else
            {
                Debug.WriteLine($"Unknown key token: {token}");
                return;
            }
        }

        var inputs = new List<INPUT>();

        foreach (var stroke in strokes)
        {
            inputs.Add(CreateVirtualKeyInput(stroke.VirtualKey, false, stroke.Extended));
        }

        for (int i = strokes.Count - 1; i >= 0; i--)
        {
            var stroke = strokes[i];
            inputs.Add(CreateVirtualKeyInput(stroke.VirtualKey, true, stroke.Extended));
        }

        SendInputChecked(inputs);
        Debug.WriteLine($"Sent key combo: {keyCombo}");
    }

    private void SendText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.WriteLine("Text is not specified");
            return;
        }

        foreach (char c in text)
        {
            var down = CreateUnicodeInput(c, false);
            var up = CreateUnicodeInput(c, true);
            SendInputChecked(new[] { down, up });
        }

        Debug.WriteLine($"Sent text ({text.Length} chars)");
    }

    /// <summary>
    /// Map key name to Windows virtual key code
    /// </summary>
    private bool TryGetVirtualKeyCode(string keyName, out KeyStroke stroke)
    {
        stroke = keyName.ToUpperInvariant() switch
        {
            // Modifier keys
            "CTRL" or "CONTROL" => new KeyStroke(0x11, false),
            "SHIFT" => new KeyStroke(0x10, false),
            "ALT" => new KeyStroke(0x12, false),
            "WIN" or "WINDOWS" => new KeyStroke(0x5B, false),

            // Function keys
            "F1" => new KeyStroke(0x70, false),
            "F2" => new KeyStroke(0x71, false),
            "F3" => new KeyStroke(0x72, false),
            "F4" => new KeyStroke(0x73, false),
            "F5" => new KeyStroke(0x74, false),
            "F6" => new KeyStroke(0x75, false),
            "F7" => new KeyStroke(0x76, false),
            "F8" => new KeyStroke(0x77, false),
            "F9" => new KeyStroke(0x78, false),
            "F10" => new KeyStroke(0x79, false),
            "F11" => new KeyStroke(0x7A, false),
            "F12" => new KeyStroke(0x7B, false),

            // Special keys
            "ESC" or "ESCAPE" => new KeyStroke(0x1B, false),
            "TAB" => new KeyStroke(0x09, false),
            "ENTER" or "RETURN" => new KeyStroke(0x0D, false),
            "SPACE" => new KeyStroke(0x20, false),
            "BACKSPACE" => new KeyStroke(0x08, false),
            "DELETE" or "DEL" => new KeyStroke(0x2E, true),
            "INSERT" or "INS" => new KeyStroke(0x2D, true),
            "HOME" => new KeyStroke(0x24, true),
            "END" => new KeyStroke(0x23, true),
            "PAGEUP" or "PGUP" => new KeyStroke(0x21, true),
            "PAGEDOWN" or "PGDN" => new KeyStroke(0x22, true),
            "UP" => new KeyStroke(0x26, true),
            "DOWN" => new KeyStroke(0x28, true),
            "LEFT" => new KeyStroke(0x25, true),
            "RIGHT" => new KeyStroke(0x27, true),

            // Letters (A-Z)
            string s when s.Length == 1 && char.IsLetter(s[0]) => new KeyStroke((ushort)s[0], false),

            // Numbers (0-9)
            string s when s.Length == 1 && char.IsDigit(s[0]) => new KeyStroke((ushort)s[0], false),

            _ => default
        };

        return stroke.VirtualKey != 0;
    }

    private static INPUT CreateVirtualKeyInput(ushort virtualKey, bool keyUp, bool extended)
    {
        uint flags = 0;
        if (extended)
        {
            flags |= KEYEVENTF_EXTENDEDKEY;
        }

        if (keyUp)
        {
            flags |= KEYEVENTF_KEYUP;
        }

        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static INPUT CreateUnicodeInput(char c, bool keyUp)
    {
        uint flags = KEYEVENTF_UNICODE;
        if (keyUp)
        {
            flags |= KEYEVENTF_KEYUP;
        }

        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static void SendInputChecked(IReadOnlyList<INPUT> inputs)
    {
        uint sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count)
        {
            int error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"SendInput sent {sent}/{inputs.Count}. Win32Error={error}");
        }
    }

    private static void StartProcess(string fileName, string arguments, ButtonAction action)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = action.UseShellExecute
        };

        if (!string.IsNullOrWhiteSpace(action.WorkingDirectory))
        {
            startInfo.WorkingDirectory = action.WorkingDirectory;
        }

        var process = Process.Start(startInfo);
        if (process != null && action.WaitForExit)
        {
            process.WaitForExit();
        }
    }

    private static void LaunchViaCmdStart(string fileName, string arguments, string? workingDirectory)
    {
        string commandToRun = BuildCommandForStart(fileName, arguments);
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/d /c start \"\" {commandToRun}",
            UseShellExecute = false
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        Process.Start(startInfo);
    }

    private static bool TryLaunchWindowsTerminal(string arguments, string? workingDirectory)
    {
        try
        {
            string aliasPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "WindowsApps",
                "wt.exe"
            );

            if (File.Exists(aliasPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = aliasPath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                        : workingDirectory
                });
                return true;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "shell:AppsFolder\\Microsoft.WindowsTerminal_8wekyb3d8bbwe!App",
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsWindowsTerminalAlias(string fileName)
    {
        string normalized = Path.GetFileName(fileName).ToLowerInvariant();
        return normalized is "wt" or "wt.exe";
    }

    private static string BuildCommandForStart(string fileName, string arguments)
    {
        var builder = new StringBuilder();
        builder.Append(QuoteIfNeeded(fileName));
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            builder.Append(' ');
            builder.Append(arguments);
        }

        return builder.ToString();
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        if (value.Contains(' ') && !(value.StartsWith('"') && value.EndsWith('"')))
        {
            return $"\"{value}\"";
        }

        return value;
    }
}
