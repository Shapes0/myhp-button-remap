using System.Text.Json.Serialization;

namespace HPButtonRemap;

/// <summary>
/// Configuration model for HP button remap application
/// </summary>
public class Config
{
    public bool ShowStartupNotification { get; set; } = true;
    public bool RunAtStartup { get; set; } = true;
    public int? EventID { get; set; } = 29;
    public int? EventData { get; set; } = 8616;
    public ButtonAction Action { get; set; } = ButtonAction.CreateDefault();

    [JsonIgnore]
    public int EffectiveEventID => EventID ?? 29;

    [JsonIgnore]
    public int EffectiveEventData => EventData ?? 8616;
}

/// <summary>
/// Represents an action to perform when a button is pressed
/// </summary>
public class ButtonAction
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionType Type { get; set; } = ActionType.RunCommand;

    public string? Command { get; set; }
    public bool? CreateNewWindow { get; set; } = true;
    public string? WorkingDirectory { get; set; }
    public int? DelayMs { get; set; } = 0;
    public string? RemapKey { get; set; }
    public string? SendText { get; set; }
    public string? OpenWebsite { get; set; }

    [JsonIgnore]
    public bool EffectiveCreateNewWindow => CreateNewWindow ?? true;

    [JsonIgnore]
    public int EffectiveDelayMs => DelayMs ?? 0;

    public static ButtonAction CreateDefault()
    {
        return new ButtonAction
        {
            Type = ActionType.RunCommand,
            Command = "%localappdata%\\Microsoft\\WindowsApps\\wt.exe",
            CreateNewWindow = true
        };
    }
}

/// <summary>
/// Types of actions that can be performed
/// </summary>
public enum ActionType
{
    RemapKey,
    SendText,
    RunCommand,
    OpenWebsite
}
