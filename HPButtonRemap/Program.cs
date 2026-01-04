using HPButtonRemap;
using System.Diagnostics;

namespace HPButtonRemap;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext());
    }
}

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon _trayIcon;
    private WmiEventMonitor? _monitor;
    private ActionExecutor _executor;
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json"
    );

    public TrayApplicationContext()
    {
        _executor = new ActionExecutor();
        
        // Create tray icon
        _trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true,
            Text = "HP Button Remap"
        };

        // Load config and start monitoring
        StartMonitoring();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        
        var openConfigItem = new ToolStripMenuItem("Open Configuration");
        openConfigItem.Click += (s, e) => OpenConfiguration();
        menu.Items.Add(openConfigItem);

        var openConfiguratorItem = new ToolStripMenuItem("Open Configurator");
        openConfiguratorItem.Click += (s, e) => OpenConfigurator();
        menu.Items.Add(openConfiguratorItem);

        menu.Items.Add(new ToolStripSeparator());

        var reloadItem = new ToolStripMenuItem("Reload Configuration");
        reloadItem.Click += (s, e) => ReloadConfiguration();
        menu.Items.Add(reloadItem);

        menu.Items.Add(new ToolStripSeparator());

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (s, e) => ShowAbout();
        menu.Items.Add(aboutItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Exit();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void StartMonitoring()
    {
        try
        {
            var config = LoadConfiguration();
            if (config == null || config.ButtonActions.Count == 0)
            {
                _trayIcon.ShowBalloonTip(5000, "HP Button Remap", 
                    "No valid button actions configured. Please configure using the configurator.", 
                    ToolTipIcon.Warning);
                return;
            }

            _monitor?.Dispose();
            _monitor = new WmiEventMonitor(_executor);
            _monitor.StartMonitoring(config);

            _trayIcon.ShowBalloonTip(2000, "HP Button Remap", 
                $"Monitoring {config.ButtonActions.Count} button action(s)", 
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting monitoring: {ex.Message}", 
                "HP Button Remap Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
    }

    private Config? LoadConfiguration()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                CreateSampleConfig();
                return LoadConfiguration();
            }

            var json = File.ReadAllText(ConfigPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);
            return config;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading configuration: {ex.Message}", 
                "HP Button Remap Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
            return null;
        }
    }

    private void CreateSampleConfig()
    {
        var sampleConfig = new Config
        {
            ButtonActions = new List<ButtonAction>
            {
                new ButtonAction
                {
                    Name = "F11 Key - Launch Notepad",
                    EventID = 29,
                    EventData = 8616,
                    Type = ActionType.LaunchApp,
                    LaunchPath = "notepad.exe",
                    LaunchArguments = ""
                }
            }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(sampleConfig, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    private void OpenConfiguration()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ConfigPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening configuration: {ex.Message}", 
                "HP Button Remap Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
    }

    private void OpenConfigurator()
    {
        try
        {
            var configuratorPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "HPButtonRemapConfig.exe"
            );

            if (File.Exists(configuratorPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = configuratorPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Configurator not found. Please use 'Open Configuration' to edit manually.", 
                    "HP Button Remap", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening configurator: {ex.Message}", 
                "HP Button Remap Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
    }

    private void ReloadConfiguration()
    {
        StartMonitoring();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "HP Button Remap\n\n" +
            "Monitors HP laptop special function keys and executes configured actions.\n\n" +
            "Configuration: " + ConfigPath + "\n\n" +
            "Right-click the tray icon to access options.",
            "About HP Button Remap",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void Exit()
    {
        _monitor?.Dispose();
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _monitor?.Dispose();
            _trayIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
