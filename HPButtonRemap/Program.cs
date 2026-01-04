using HPButtonRemap;
using Newtonsoft.Json;

class Program
{
    private static WmiEventMonitor? _monitor;
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json"
    );

    static void Main(string[] args)
    {
        Console.WriteLine("=== HP Button Remap - Native Windows Application ===");
        Console.WriteLine();

        // Load configuration
        var config = LoadConfiguration();
        if (config == null || config.ButtonActions.Count == 0)
        {
            Console.WriteLine("[ERROR] No valid button actions configured");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Set up console cancellation handler
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine();
            Console.WriteLine("Shutdown requested...");
            _monitor?.Dispose();
            Environment.Exit(0);
        };

        try
        {
            // Create executor and monitor
            var executor = new ActionExecutor();
            _monitor = new WmiEventMonitor(executor);

            // Start monitoring
            _monitor.StartMonitoring(config);

            Console.WriteLine();
            Console.WriteLine("=== Monitoring Active ===");
            Console.WriteLine("Press Ctrl+C to stop");
            Console.WriteLine();

            // Keep running
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fatal error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        finally
        {
            _monitor?.Dispose();
        }
    }

    /// <summary>
    /// Load configuration from JSON file
    /// </summary>
    private static Config? LoadConfiguration()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Console.WriteLine($"[ERROR] Configuration file not found: {ConfigPath}");
                Console.WriteLine("Creating sample configuration...");
                CreateSampleConfig();
                Console.WriteLine($"[OK] Sample config created at: {ConfigPath}");
                Console.WriteLine("Please edit the configuration file and restart the application");
                return null;
            }

            var json = File.ReadAllText(ConfigPath);
            var config = JsonConvert.DeserializeObject<Config>(json);

            if (config == null)
            {
                Console.WriteLine("[ERROR] Failed to parse configuration file");
                return null;
            }

            Console.WriteLine($"[OK] Configuration loaded: {config.ButtonActions.Count} action(s)");
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to load configuration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a sample configuration file
    /// </summary>
    private static void CreateSampleConfig()
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

        var json = JsonConvert.SerializeObject(sampleConfig, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
        
        Console.WriteLine();
        Console.WriteLine("Sample configuration created. See CONFIG-EXAMPLES.md for more examples.");
    }
}
