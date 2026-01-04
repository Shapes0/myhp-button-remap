using System.Management;

namespace HPButtonRemap;

/// <summary>
/// Monitors HP WMI events and triggers actions
/// </summary>
public class WmiEventMonitor : IDisposable
{
    private readonly ActionExecutor _executor;
    private readonly List<ManagementEventWatcher> _watchers = new();
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
        Console.WriteLine("Starting HP WMI Event Monitor...");

        foreach (var action in config.ButtonActions)
        {
            try
            {
                RegisterEventHandler(action);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to register action '{action.Name}': {ex.Message}");
            }
        }

        if (_watchers.Count == 0)
        {
            Console.WriteLine("[WARNING] No event handlers registered!");
        }
        else
        {
            Console.WriteLine($"[OK] Monitoring {_watchers.Count} button action(s)");
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

        Console.WriteLine($"[OK] Registered: {action.Name} (EventID: {action.EventID}, EventData: {action.EventData})");
    }

    /// <summary>
    /// Handle WMI event arrival
    /// </summary>
    private void OnEventArrived(ButtonAction action, EventArrivedEventArgs e)
    {
        try
        {
            // Execute the configured action
            _executor.ExecuteAction(action);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Event handler error for '{action.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Stop monitoring and cleanup
    /// </summary>
    public void StopMonitoring()
    {
        Console.WriteLine("Stopping HP WMI Event Monitor...");

        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.Stop();
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop watcher: {ex.Message}");
            }
        }

        _watchers.Clear();
        Console.WriteLine("[OK] Monitoring stopped");
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
