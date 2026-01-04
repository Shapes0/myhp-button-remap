using HPButtonRemap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "HP Button Remap Service";
});

builder.Services.AddHostedService<HPButtonRemapService>();
builder.Services.AddSingleton<ActionExecutor>();

var host = builder.Build();
await host.RunAsync();

public class HPButtonRemapService : BackgroundService
{
    private readonly ILogger<HPButtonRemapService> _logger;
    private readonly ActionExecutor _executor;
    private WmiEventMonitor? _monitor;
    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json"
    );

    public HPButtonRemapService(ILogger<HPButtonRemapService> logger, ActionExecutor executor)
    {
        _logger = logger;
        _executor = executor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("HP Button Remap Service starting...");

            // Load configuration
            var config = LoadConfiguration();
            if (config == null || config.ButtonActions.Count == 0)
            {
                _logger.LogError("No valid button actions configured");
                return;
            }

            // Start monitoring
            _monitor = new WmiEventMonitor(_executor, _logger);
            _monitor.StartMonitoring(config);

            _logger.LogInformation("HP Button Remap Service is running");

            // Keep service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in service");
            throw;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HP Button Remap Service stopping...");
        _monitor?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private Config? LoadConfiguration()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                _logger.LogError("Configuration file not found: {ConfigPath}", ConfigPath);
                CreateSampleConfig();
                return null;
            }

            var json = File.ReadAllText(ConfigPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);

            if (config == null)
            {
                _logger.LogError("Failed to parse configuration file");
                return null;
            }

            _logger.LogInformation("Configuration loaded: {Count} action(s)", config.ButtonActions.Count);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
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
        _logger.LogInformation("Sample configuration created at: {ConfigPath}", ConfigPath);
    }
}
