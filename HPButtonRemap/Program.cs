using System.Diagnostics;
using System.Text.Json;

namespace HPButtonRemap;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (SelfBootstrapper.TryInstallAndRelaunch(args))
        {
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext());
    }
}

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ActionExecutor _executor;
    private readonly ConfigStore _configStore;
    private readonly StartupShortcutManager _startupShortcutManager;
    private readonly ToolStripMenuItem _runAtStartupItem;

    private WmiEventMonitor? _monitor;
    private Config? _currentConfig;

    public TrayApplicationContext()
    {
        _executor = new ActionExecutor();
        _configStore = new ConfigStore(AppPaths.ConfigPath);
        _startupShortcutManager = new StartupShortcutManager();

        _runAtStartupItem = new ToolStripMenuItem("Run at startup")
        {
            CheckOnClick = true
        };
        _runAtStartupItem.Click += (_, _) => ToggleRunAtStartup();

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true,
            Text = "HP Button Remap"
        };

        ReloadConfiguration();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var openConfigItem = new ToolStripMenuItem("Open Configuration");
        openConfigItem.Click += (_, _) => OpenConfiguration();
        menu.Items.Add(openConfigItem);

        var reloadItem = new ToolStripMenuItem("Reload Configuration");
        reloadItem.Click += (_, _) => ReloadConfiguration();
        menu.Items.Add(reloadItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_runAtStartupItem);

        menu.Items.Add(new ToolStripSeparator());

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (_, _) => ShowAbout();
        menu.Items.Add(aboutItem);

        var uninstallItem = new ToolStripMenuItem("Uninstall...");
        uninstallItem.Click += (_, _) => ShowUninstall();
        menu.Items.Add(uninstallItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ReloadConfiguration()
    {
        try
        {
            _monitor?.Dispose();

            _currentConfig = _configStore.LoadOrCreateDefault();
            _runAtStartupItem.Checked = _currentConfig.RunAtStartup;
            _startupShortcutManager.SetStartupEnabled(
                _currentConfig.RunAtStartup,
                AppPaths.ExecutablePath,
                AppPaths.AppDirectory
            );

            _monitor = new WmiEventMonitor(_executor);
            _monitor.StartMonitoring(_currentConfig);

            if (_currentConfig.Action == null)
            {
                _trayIcon.ShowBalloonTip(
                    5000,
                    "HP Button Remap",
                    "No action configured. Open config.json to add an Action object.",
                    ToolTipIcon.Warning
                );
                return;
            }

            if (_currentConfig.ShowStartupNotification)
            {
                _trayIcon.ShowBalloonTip(
                    2000,
                    "HP Button Remap",
                    $"Monitoring EventID={_currentConfig.EffectiveEventID}, EventData={_currentConfig.EffectiveEventData}",
                    ToolTipIcon.Info
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error reloading configuration: {ex.Message}",
                "HP Button Remap Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void ToggleRunAtStartup()
    {
        if (_currentConfig == null)
        {
            return;
        }

        bool previous = _currentConfig.RunAtStartup;
        _currentConfig.RunAtStartup = _runAtStartupItem.Checked;
        try
        {
            _startupShortcutManager.SetStartupEnabled(
                _currentConfig.RunAtStartup,
                AppPaths.ExecutablePath,
                AppPaths.AppDirectory
            );
            _configStore.Save(_currentConfig);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to update startup setting: {ex.Message}",
                "HP Button Remap Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            _currentConfig.RunAtStartup = previous;
            _runAtStartupItem.Checked = previous;
        }
    }

    private static void OpenConfiguration()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.ConfigPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to open configuration file: {ex.Message}",
                "HP Button Remap Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void ShowAbout()
    {
        string statusText = _currentConfig is { Action: not null }
            ? $"Status: Monitoring EventID={_currentConfig.EffectiveEventID}, EventData={_currentConfig.EffectiveEventData} ({_currentConfig.Action.Type})"
            : "Status: No actions configured";

        MessageBox.Show(
            "HP Button Remap\n\n" +
            "Actions: RemapKey, SendText, RunCommand, OpenWebsite\n\n" +
            statusText + "\n\n" +
            "Config: " + AppPaths.ConfigPath + "\n\n" +
            "Right-click the tray icon to access options.",
            "About HP Button Remap",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    private void ShowUninstall()
    {
        var result = MessageBox.Show(
            "This removes HP Button Remap and its startup shortcut.\n\n" +
            "Your config will be backed up to the Desktop before removal.\n\n" +
            "Continue?",
            "Uninstall HP Button Remap",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            PerformUninstall();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error during uninstall: {ex.Message}",
                "Uninstall Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void PerformUninstall()
    {
        _startupShortcutManager.SetStartupEnabled(false, AppPaths.ExecutablePath, AppPaths.AppDirectory);

        // Only self-delete if running from the managed install directory.
        if (!AppPaths.IsRunningFromManagedInstallDirectory)
        {
            MessageBox.Show(
                "Startup registration has been removed.\n\n" +
                "This app is running in portable mode; delete its folder manually to finish uninstall.",
                "Portable Mode",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            ExitApplication();
            return;
        }

        if (File.Exists(AppPaths.ConfigPath))
        {
            string backupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "HPButtonRemap-config-backup.json"
            );
            File.Copy(AppPaths.ConfigPath, backupPath, true);
        }

        string batchPath = Path.Combine(Path.GetTempPath(), "HPButtonRemap-uninstall.bat");
        string batchContent = $@"@echo off
timeout /t 2 /nobreak >nul
rd /s /q ""{AppPaths.InstallDirectory}""
del ""{batchPath}""
";
        File.WriteAllText(batchPath, batchContent);

        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        MessageBox.Show(
            "Uninstall started. The app will now close and remove its installed files.",
            "Uninstall Complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        ExitApplication();
    }

    private void ExitApplication()
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
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class AppPaths
{
    public static readonly string InstallDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HPButtonRemap"
    );

    public static readonly string InstalledExecutablePath = Path.Combine(InstallDirectory, "HPButtonRemap.exe");

    public static string AppDirectory => Path.GetFullPath(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
    public static string ExecutablePath => Environment.ProcessPath ?? InstalledExecutablePath;
    public static string ConfigPath => Path.Combine(AppDirectory, "config.json");
    public static string StartupShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "HP Button Remap.lnk"
    );

    public static bool IsRunningFromManagedInstallDirectory =>
        string.Equals(
            NormalizePath(AppDirectory),
            NormalizePath(InstallDirectory),
            StringComparison.OrdinalIgnoreCase
        );

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _configPath;

    public ConfigStore(string configPath)
    {
        _configPath = configPath;
    }

    public Config LoadOrCreateDefault()
    {
        if (!File.Exists(_configPath))
        {
            var defaults = CreateDefaultConfig();
            Save(defaults);
            return defaults;
        }

        string json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<Config>(json, JsonOptions) ?? CreateDefaultConfig();
    }

    public void Save(Config config)
    {
        string json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    private static Config CreateDefaultConfig()
    {
        return new Config
        {
            ShowStartupNotification = true,
            RunAtStartup = true,
            EventID = 29,
            EventData = 8616,
            Action = ButtonAction.CreateDefault()
        };
    }
}

public sealed class StartupShortcutManager
{
    public void SetStartupEnabled(bool enabled, string targetPath, string workingDirectory)
    {
        if (!enabled)
        {
            if (File.Exists(AppPaths.StartupShortcutPath))
            {
                File.Delete(AppPaths.StartupShortcutPath);
            }
            return;
        }

        var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
        var shortcut = shell!.GetType().InvokeMember(
            "CreateShortcut",
            System.Reflection.BindingFlags.InvokeMethod,
            null,
            shell,
            new object[] { AppPaths.StartupShortcutPath }
        );

        shortcut!.GetType().InvokeMember(
            "TargetPath",
            System.Reflection.BindingFlags.SetProperty,
            null,
            shortcut,
            new object[] { targetPath }
        );
        shortcut.GetType().InvokeMember(
            "WorkingDirectory",
            System.Reflection.BindingFlags.SetProperty,
            null,
            shortcut,
            new object[] { workingDirectory }
        );
        shortcut.GetType().InvokeMember(
            "Description",
            System.Reflection.BindingFlags.SetProperty,
            null,
            shortcut,
            new object[] { "HP Button Remap" }
        );
        shortcut.GetType().InvokeMember(
            "Save",
            System.Reflection.BindingFlags.InvokeMethod,
            null,
            shortcut,
            null
        );
    }
}

public static class SelfBootstrapper
{
    public static bool TryInstallAndRelaunch(string[] args)
    {
#if DEBUG
        return false;
#else
        if (args.Any(arg => string.Equals(arg, "--portable", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        string currentExe = Environment.ProcessPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentExe))
        {
            return false;
        }

        string currentPath = Path.GetFullPath(currentExe);
        string installedPath = Path.GetFullPath(AppPaths.InstalledExecutablePath);

        if (string.Equals(currentPath, installedPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(AppPaths.InstallDirectory);
            File.Copy(currentPath, installedPath, true);

            string sourceConfig = Path.Combine(Path.GetDirectoryName(currentPath)!, "config.json");
            string targetConfig = Path.Combine(AppPaths.InstallDirectory, "config.json");
            if (File.Exists(sourceConfig) && !File.Exists(targetConfig))
            {
                File.Copy(sourceConfig, targetConfig, false);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installedPath,
                UseShellExecute = true,
                WorkingDirectory = AppPaths.InstallDirectory
            });

            return true;
        }
        catch
        {
            // If installation bootstrap fails, continue in portable mode.
            return false;
        }

#endif
    }
}
