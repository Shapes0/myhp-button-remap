using System.Management;
using Microsoft.Extensions.Logging;

namespace HPButtonRemap;

/// <summary>
/// Monitors HP WMI events and triggers actions
/// </summary>
public class WmiEventMonitor : IDisposable
{
    private readonly ActionExecutor _executor;
    private readonly ILogger _logger;
    private readonly List<ManagementEventWatcher> _watchers = new();
    private bool _disposed;

    public WmiEventMonitor(ActionExecutor executor, ILogger logger)
    {
        _executor = executor;
        _logger = logger;
    }

    /// <summary>
    /// Start monitoring for configured button events
    /// </summary>
    public void StartMonitoring(Config config)
    {
        _logger.LogInformation("Starting HP WMI Event Monitor...");

        foreach (var action in config.ButtonActions)
        {
            try
            {
                RegisterEventHandler(action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register action '{ActionName}'", action.Name);
            }
        }

        if (_watchers.Count == 0)
        {
            _logger.LogWarning("No event handlers registered!");
        }
        else
        {
            _logger.LogInformation("Monitoring {Count} button action(s)", _watchers.Count);
        }
    }

    /// <summary>
    /// Register a WMI event handler for a specific button action
    /// </summary>
    private void RegisterEventHandler(ButtonAction action)
    {
        // Build WQL query to filter HP button events
        string query = $"SELECT * FROM hpqBEvnt WHERE EventID = {action.EventID}";
        
        if (action.EventData != 0)
        {
            query += $" AND EventData = {action.EventData}";
        }

        var scope = new ManagementScope(@"root\wmi");
        var eventQuery = new WqlEventQuery(query);
        var watcher = new ManagementEventWatcher(scope, eventQuery);

        // Set up event handler
        watcher.EventArrived += (sender, e) =>
        {
            OnEventArrived(action, e);
        };

        // Start watching
        watcher.Start();
        _watchers.Add(watcher);

        _logger.LogInformation("Registered: {ActionName} (EventID: {EventID}, EventData: {EventData})", 
            action.Name, action.EventID, action.EventData);
    }

    /// <summary>
    /// Handle WMI event arrival
    /// </summary>
    private void OnEventArrived(ButtonAction action, EventArrivedEventArgs e)
    {
        try
        {
            // Execute the configured action
            _executor.ExecuteAction(action, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event handler error for '{ActionName}'", action.Name);
        }
    }

    /// <summary>
    /// Stop monitoring and cleanup
    /// </summary>
    public void StopMonitoring()
    {
        _logger.LogInformation("Stopping HP WMI Event Monitor...");

        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.Stop();
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop watcher");
            }
        }

        _watchers.Clear();
        _logger.LogInformation("Monitoring stopped");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopMonitoring();
            _disposed = true;
        }
    }
}
