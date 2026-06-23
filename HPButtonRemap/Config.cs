using System.Text.Json.Serialization;

namespace HPButtonRemap;

/// <summary>
/// Configuration model for HP button remap application
/// </summary>
public class Config
{
    public List<ButtonAction> ButtonActions { get; set; } = new();
    public bool ShowStartupNotification { get; set; } = true;
}

/// <summary>
/// Represents an action to perform when a button is pressed
/// </summary>
public class ButtonAction
{
    public string Name { get; set; } = string.Empty;
    public int EventID { get; set; }
    public int EventData { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionType Type { get; set; }

    // Legacy fields kept for backward compatibility
    public string? LaunchPath { get; set; }
    public string? LaunchArguments { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? KeyCombo { get; set; }

    // New simplified fields
    public string? ProgramPath { get; set; }
    public string? ProgramArguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? Url { get; set; }
    public string? RemapTo { get; set; }
    public string? Text { get; set; }
    public string? Command { get; set; }

    // Optional behavior flags
    public bool UseShellExecute { get; set; } = true;
    public bool CreateNewWindow { get; set; } = true;
    public bool WaitForExit { get; set; } = false;
    public int DelayMs { get; set; } = 0;

    [JsonIgnore]
    public string? EffectiveProgramPath => string.IsNullOrWhiteSpace(ProgramPath) ? LaunchPath : ProgramPath;

    [JsonIgnore]
    public string? EffectiveProgramArguments => string.IsNullOrWhiteSpace(ProgramArguments) ? LaunchArguments : ProgramArguments;

    [JsonIgnore]
    public string? EffectiveUrl => string.IsNullOrWhiteSpace(Url) ? WebsiteUrl : Url;

    [JsonIgnore]
    public string? EffectiveKeyCombo => string.IsNullOrWhiteSpace(RemapTo) ? KeyCombo : RemapTo;
}

/// <summary>
/// Types of actions that can be performed
/// </summary>
public enum ActionType
{
    RemapKey,
    SendText,
    RunCommand,
    LaunchApp,
    OpenWebsite,
    SendKeys
}
