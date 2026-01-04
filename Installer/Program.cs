using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPButtonRemapInstaller;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // Check if running as uninstaller
        if (args.Length > 0 && args[0] == "--uninstall")
        {
            Application.Run(new UninstallerForm());
        }
        else
        {
            Application.Run(new InstallerForm());
        }
    }
}

public class InstallerForm : Form
{
    private Label statusLabel;
    private ProgressBar progressBar;
    private Button installButton;
    private Button cancelButton;
    private TextBox logTextBox;

    public InstallerForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "HP Button Remap - Installer";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var titleLabel = new Label
        {
            Text = "HP Button Remap Installer",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Location = new Point(20, 20),
            Size = new Size(560, 30)
        };

        var descLabel = new Label
        {
            Text = "This will install HP Button Remap to your system.\n\n" +
                   "The application will:\n" +
                   "• Install to %LOCALAPPDATA%\\HPButtonRemap\n" +
                   "• Add a shortcut to your Startup folder (auto-start)\n" +
                   "• Add a configurator to your Start Menu\n" +
                   "• Start the tray application immediately",
            Location = new Point(20, 60),
            Size = new Size(560, 120),
            AutoSize = false
        };

        statusLabel = new Label
        {
            Text = "Ready to install",
            Location = new Point(20, 190),
            Size = new Size(560, 20)
        };

        progressBar = new ProgressBar
        {
            Location = new Point(20, 220),
            Size = new Size(560, 23),
            Visible = false
        };

        logTextBox = new TextBox
        {
            Location = new Point(20, 253),
            Size = new Size(560, 150),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            Visible = false
        };

        installButton = new Button
        {
            Text = "Install",
            Location = new Point(400, 420),
            Size = new Size(90, 30)
        };
        installButton.Click += InstallButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(500, 420),
            Size = new Size(80, 30)
        };
        cancelButton.Click += (s, e) => Close();

        Controls.AddRange(new Control[] { titleLabel, descLabel, statusLabel, progressBar, logTextBox, installButton, cancelButton });
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        installButton.Enabled = false;
        cancelButton.Enabled = false;
        progressBar.Visible = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        logTextBox.Visible = true;

        try
        {
            await Task.Run(() => PerformInstallation());
            
            statusLabel.Text = "Installation complete!";
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;

            MessageBox.Show(
                "HP Button Remap has been installed successfully!\n\n" +
                "The tray application is now running. Look for the icon in your system tray.\n\n" +
                "Right-click the tray icon to configure button actions.",
                "Installation Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Close();
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Installation failed!";
            progressBar.Visible = false;
            MessageBox.Show(
                $"Installation failed:\n\n{ex.Message}",
                "Installation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            installButton.Enabled = true;
            cancelButton.Enabled = true;
        }
    }

    private void PerformInstallation()
    {
        string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPButtonRemap");
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, "HP Button Remap.lnk");

        Log("Starting installation...");

        // Create installation directory
        Log($"Creating installation directory: {installDir}");
        Directory.CreateDirectory(installDir);

        // Extract embedded resources
        Log("Extracting application files...");
        ExtractResource("HPButtonRemap.exe", Path.Combine(installDir, "HPButtonRemap.exe"));
        Log("  - HPButtonRemap.exe");
        
        ExtractResource("HPButtonRemapConfig.exe", Path.Combine(installDir, "HPButtonRemapConfig.exe"));
        Log("  - HPButtonRemapConfig.exe");

        string destConfig = Path.Combine(installDir, "config.json");
        if (!File.Exists(destConfig))
        {
            ExtractResource("config.json", destConfig);
            Log("  - config.json");
        }
        else
        {
            Log("  - config.json (existing config preserved)");
        }

        // Create startup shortcut
        Log("Creating startup shortcut...");
        CreateShortcut(shortcutPath, Path.Combine(installDir, "HPButtonRemap.exe"), installDir, "HP Button Remap - Monitors HP special function keys");

        // Create Start Menu shortcut for configurator
        if (File.Exists(Path.Combine(installDir, "HPButtonRemapConfig.exe")))
        {
            Log("Creating Start Menu shortcut...");
            string startMenuFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string startMenuShortcut = Path.Combine(startMenuFolder, "HP Button Remap Configurator.lnk");
            CreateShortcut(startMenuShortcut, Path.Combine(installDir, "HPButtonRemapConfig.exe"), installDir, "HP Button Remap Configurator");
        }

        // Start the application
        Log("Starting HP Button Remap...");
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(installDir, "HPButtonRemap.exe"),
            WorkingDirectory = installDir,
            UseShellExecute = true
        });

        Log("Installation complete!");
    }

    private void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
    {
        var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
        var shortcut = shell!.GetType().InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
        
        shortcut!.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
        shortcut.GetType().InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
        shortcut.GetType().InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { description });
        shortcut.GetType().InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
    }

    private void ExtractResource(string resourceName, string destinationPath)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourcePath = $"HPButtonRemapInstaller.Resources.{resourceName}";
        
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
        }
        
        using var fileStream = File.Create(destinationPath);
        stream.CopyTo(fileStream);
    }

    private void Log(string message)
    {
        if (logTextBox.InvokeRequired)
        {
            logTextBox.Invoke(() => Log(message));
        }
        else
        {
            logTextBox.AppendText(message + Environment.NewLine);
        }
    }
}

public class UninstallerForm : Form
{
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private Button uninstallButton = null!;
    private Button cancelButton = null!;
    private TextBox logTextBox = null!;
    private CheckBox keepConfigCheckBox = null!;

    public UninstallerForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "HP Button Remap - Uninstaller";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var titleLabel = new Label
        {
            Text = "HP Button Remap Uninstaller",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Location = new Point(20, 20),
            Size = new Size(560, 30)
        };

        var descLabel = new Label
        {
            Text = "This will uninstall HP Button Remap from your system.\n\n" +
                   "The following will be removed:\n" +
                   "• Startup shortcut\n" +
                   "• Start Menu shortcut\n" +
                   "• Application files",
            Location = new Point(20, 60),
            Size = new Size(560, 100),
            AutoSize = false
        };

        keepConfigCheckBox = new CheckBox
        {
            Text = "Backup configuration file to Desktop",
            Location = new Point(20, 170),
            Size = new Size(300, 20),
            Checked = true
        };

        statusLabel = new Label
        {
            Text = "Ready to uninstall",
            Location = new Point(20, 200),
            Size = new Size(560, 20)
        };

        progressBar = new ProgressBar
        {
            Location = new Point(20, 230),
            Size = new Size(560, 23),
            Visible = false
        };

        logTextBox = new TextBox
        {
            Location = new Point(20, 263),
            Size = new Size(560, 140),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            Visible = false
        };

        uninstallButton = new Button
        {
            Text = "Uninstall",
            Location = new Point(380, 420),
            Size = new Size(110, 30),
            BackColor = Color.IndianRed,
            ForeColor = Color.White
        };
        uninstallButton.Click += UninstallButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(500, 420),
            Size = new Size(80, 30)
        };
        cancelButton.Click += (s, e) => Close();

        Controls.AddRange(new Control[] { titleLabel, descLabel, keepConfigCheckBox, statusLabel, progressBar, logTextBox, uninstallButton, cancelButton });
    }

    private async void UninstallButton_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to uninstall HP Button Remap?",
            "Confirm Uninstall",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        uninstallButton.Enabled = false;
        cancelButton.Enabled = false;
        keepConfigCheckBox.Enabled = false;
        progressBar.Visible = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        logTextBox.Visible = true;

        try
        {
            await Task.Run(() => PerformUninstallation());
            
            statusLabel.Text = "Uninstallation complete!";
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;

            string message = "HP Button Remap has been uninstalled successfully!";
            if (keepConfigCheckBox.Checked)
            {
                message += "\n\nYour configuration has been backed up to your Desktop.";
            }

            MessageBox.Show(message, "Uninstallation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Uninstallation failed!";
            progressBar.Visible = false;
            MessageBox.Show(
                $"Uninstallation failed:\n\n{ex.Message}",
                "Uninstallation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            uninstallButton.Enabled = true;
            cancelButton.Enabled = true;
            keepConfigCheckBox.Enabled = true;
        }
    }

    private void PerformUninstallation()
    {
        string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPButtonRemap");
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, "HP Button Remap.lnk");
        string startMenuFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        string startMenuShortcut = Path.Combine(startMenuFolder, "HP Button Remap Configurator.lnk");

        Log("Starting uninstallation...");

        // Stop the application
        Log("Stopping HP Button Remap...");
        foreach (var process in Process.GetProcessesByName("HPButtonRemap"))
        {
            try
            {
                process.Kill();
                process.WaitForExit(5000);
                Log("  - Application stopped");
            }
            catch { }
        }
        Thread.Sleep(1000);

        // Backup config if requested
        if (keepConfigCheckBox.Checked)
        {
            string configPath = Path.Combine(installDir, "config.json");
            if (File.Exists(configPath))
            {
                Log("Backing up configuration...");
                string backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HPButtonRemap-config-backup.json");
                File.Copy(configPath, backupPath, true);
                Log($"  - Configuration backed up to Desktop");
            }
        }

        // Remove startup shortcut
        if (File.Exists(shortcutPath))
        {
            Log("Removing startup shortcut...");
            File.Delete(shortcutPath);
        }

        // Remove Start Menu shortcut
        if (File.Exists(startMenuShortcut))
        {
            Log("Removing Start Menu shortcut...");
            File.Delete(startMenuShortcut);
        }

        // Remove installation directory
        if (Directory.Exists(installDir))
        {
            Log("Removing installation directory...");
            try
            {
                Directory.Delete(installDir, true);
                Log("  - Installation directory removed");
            }
            catch (Exception ex)
            {
                Log($"  - Warning: Could not remove directory: {ex.Message}");
                Log("  - Files will be removed after reboot");
            }
        }

        Log("Uninstallation complete!");
    }

    private void Log(string message)
    {
        if (logTextBox.InvokeRequired)
        {
            logTextBox.Invoke(() => Log(message));
        }
        else
        {
            logTextBox.AppendText(message + Environment.NewLine);
        }
    }
}
