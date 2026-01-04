using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
    
    [JsonConverter(typeof(StringEnumConverter))]
    public ActionType Type { get; set; }
    
    public string? LaunchPath { get; set; }
    public string? LaunchArguments { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? KeyCombo { get; set; }
}

/// <summary>
/// Types of actions that can be performed
/// </summary>
public enum ActionType
{
    LaunchApp,
    OpenWebsite,
    SendKeys
}
