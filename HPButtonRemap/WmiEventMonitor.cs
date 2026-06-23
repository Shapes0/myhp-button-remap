using System.Management;
using System.Diagnostics;

namespace HPButtonRemap;

/// <summary>
/// Monitors HP WMI events and triggers actions
/// </summary>
public class WmiEventMonitor : IDisposable
{
    private readonly ActionExecutor _executor;
    private ManagementEventWatcher? _watcher;
    private ButtonAction? _action;
    private bool _disposed;

    public WmiEventMonitor(ActionExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Start monitoring for configured button events
    /// </summary>
    public void StartMonitoring(Config config)
    {
        Debug.WriteLine("Starting HP WMI Event Monitor...");
        StopMonitoring();

        if (config.Action == null)
        {
            Debug.WriteLine("No action configured.");
            return;
        }

        try
        {
            RegisterEventHandler(config);
            Debug.WriteLine($"Monitoring EventID={config.EffectiveEventID}, EventData={config.EffectiveEventData}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to register watcher: {ex.Message}");
        }
    }

    /// <summary>
    /// Register a WMI event handler for configured button event
    /// </summary>
    private void RegisterEventHandler(Config config)
    {
        // Build WQL query to filter HP button events
        string query = $"SELECT * FROM hpqBEvnt WHERE EventID = {config.EffectiveEventID}";

        if (config.EffectiveEventData != 0)
        {
            query += $" AND EventData = {config.EffectiveEventData}";
        }

        var scope = new ManagementScope(@"root\wmi");
        var eventQuery = new WqlEventQuery(query);
        _watcher = new ManagementEventWatcher(scope, eventQuery);
        _action = config.Action;

        // Set up event handler
        _watcher.EventArrived += (sender, e) =>
        {
            OnEventArrived(e);
        };

        // Start watching
        _watcher.Start();
    }

    /// <summary>
    /// Handle WMI event arrival
    /// </summary>
    private void OnEventArrived(EventArrivedEventArgs e)
    {
        try
        {
            if (_action == null)
            {
                return;
            }

            // Execute the configured action
            _executor.ExecuteAction(_action);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Event handler error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop monitoring and cleanup
    /// </summary>
    public void StopMonitoring()
    {
        Debug.WriteLine("Stopping HP WMI Event Monitor...");

        if (_watcher == null)
        {
            return;
        }

        try
        {
            _watcher.Stop();
            _watcher.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to stop watcher: {ex.Message}");
        }

        _watcher = null;
        _action = null;
        Debug.WriteLine("Monitoring stopped");
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
