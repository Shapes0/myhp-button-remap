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
    private Config? _currentConfig;
    private Thread? _ipcListenerThread;
    private volatile bool _shouldExit = false;
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json"
    );
    private const string IPC_EVENT_NAME = "HPButtonRemap_ReloadConfig";

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
        
        // Set up IPC listener for reload signals from configurator
        StartIpcListener();
    }
    
    private void StartIpcListener()
    {
        _ipcListenerThread = new Thread(() =>
        {
            while (!_shouldExit)
            {
                try
                {
                    using (var reloadEvent = new EventWaitHandle(false, EventResetMode.AutoReset, IPC_EVENT_NAME))
                    {
                        // Wait for signal with timeout so we can check _shouldExit periodically
                        if (reloadEvent.WaitOne(1000))
                        {
                            // Signal received - reload configuration on UI thread
                            if (_trayIcon.ContextMenuStrip != null && !_shouldExit)
                            {
                                _trayIcon.ContextMenuStrip.Invoke(new Action(() =>
                                {
                                    ReloadConfiguration();
                                }));
                            }
                        }
                    }
                }
                catch
                {
                    // If event creation fails, wait a bit and retry
                    Thread.Sleep(1000);
                }
            }
        })
        {
            IsBackground = true
        };
        _ipcListenerThread.Start();
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

        var uninstallItem = new ToolStripMenuItem("Uninstall...");
        uninstallItem.Click += (s, e) => ShowUninstall();
        menu.Items.Add(uninstallItem);

        menu.Items.Add(new ToolStripSeparator());

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
            _currentConfig = config;
            
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

            if (config.ShowStartupNotification)
            {
                _trayIcon.ShowBalloonTip(2000, "HP Button Remap", 
                    $"Monitoring {config.ButtonActions.Count} button action(s)", 
                    ToolTipIcon.Info);
            }
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
            ShowStartupNotification = true,
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
        string statusText = _currentConfig != null && _currentConfig.ButtonActions.Count > 0
            ? $"Status: Monitoring {_currentConfig.ButtonActions.Count} button action(s)"
            : "Status: No actions configured";

        MessageBox.Show(
            "HP Button Remap\n\n" +
            "Monitors HP laptop special function keys and executes configured actions.\n\n" +
            statusText + "\n\n" +
            "Configuration: " + ConfigPath + "\n\n" +
            "Right-click the tray icon to access options.",
            "About HP Button Remap",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowUninstall()
    {
        var result = MessageBox.Show(
            "This will uninstall HP Button Remap from your system.\n\n" +
            "The following will be removed:\n" +
            "• Startup shortcut\n" +
            "• Start Menu shortcut\n" +
            "• Application files\n\n" +
            "Your configuration file will be backed up to your Desktop.\n\n" +
            "Do you want to continue?",
            "Uninstall HP Button Remap",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            try
            {
                PerformUninstall();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during uninstallation: {ex.Message}\n\n" +
                    "You may need to manually remove files from:\n" +
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPButtonRemap"),
                    "Uninstall Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    private void PerformUninstall()
    {
        // Paths
        string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPButtonRemap");
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, "HP Button Remap.lnk");
        string startMenuFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        string startMenuShortcut = Path.Combine(startMenuFolder, "HP Button Remap Configurator.lnk");

        // Backup config
        string configPath = Path.Combine(installDir, "config.json");
        if (File.Exists(configPath))
        {
            string backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HPButtonRemap-config-backup.json");
            File.Copy(configPath, backupPath, true);
        }

        // Remove startup shortcut
        if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }

        // Remove Start Menu shortcut
        if (File.Exists(startMenuShortcut))
        {
            File.Delete(startMenuShortcut);
        }

        // Create a batch file to delete the directory after the app exits
        string batchPath = Path.Combine(Path.GetTempPath(), "HPButtonRemap-uninstall.bat");
        string batchContent = $@"@echo off
timeout /t 2 /nobreak >nul
rd /s /q ""{installDir}""
del ""{batchPath}""
";
        File.WriteAllText(batchPath, batchContent);

        // Start the batch file
        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        MessageBox.Show(
            "Uninstallation initiated successfully.\n\n" +
            "The application will now close and complete the removal.\n\n" +
            "Your configuration has been backed up to your Desktop.",
            "Uninstall Complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        // Exit the application
        Application.Exit();
    }

    private void Exit()
    {
        _shouldExit = true;
        _monitor?.Dispose();
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shouldExit = true;
            _monitor?.Dispose();
            _trayIcon?.Dispose();
            _ipcListenerThread?.Join(2000); // Wait up to 2 seconds for thread to exit
        }
        base.Dispose(disposing);
    }
}
