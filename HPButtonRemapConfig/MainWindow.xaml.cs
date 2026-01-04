using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Newtonsoft.Json;
using HPButtonRemap;

namespace HPButtonRemapConfig
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ButtonAction> _actions = new();
        private bool _showStartupNotification = true;
        private string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "HPButtonRemap",
            "config.json"
        );

        public MainWindow()
        {
            InitializeComponent();
            LoadConfiguration();
            ActionsListBox.ItemsSource = _actions;
        }

        private void LoadConfiguration()
        {
            try
            {
                // Check multiple possible locations for config file
                string[] possiblePaths = new[]
                {
                    ConfigPath,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPButtonRemap", "config.json")
                };

                string? foundPath = possiblePaths.FirstOrDefault(File.Exists);

                if (foundPath != null)
                {
                    var json = File.ReadAllText(foundPath);
                    var config = JsonConvert.DeserializeObject<Config>(json);
                    if (config != null && config.ButtonActions != null)
                    {
                        _actions.Clear();
                        foreach (var action in config.ButtonActions)
                        {
                            _actions.Add(action);
                        }
                        _showStartupNotification = config.ShowStartupNotification;
                        ShowStartupNotificationCheckBox.IsChecked = _showStartupNotification;
                    }
                }
                else
                {
                    // Create default configuration
                    _actions.Add(new ButtonAction
                    {
                        Name = "F11 Key - Launch Notepad",
                        EventID = 29,
                        EventData = 8616,
                        Type = ActionType.LaunchApp,
                        LaunchPath = "notepad.exe",
                        LaunchArguments = ""
                    });
                    ShowStartupNotificationCheckBox.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
        }
        
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfiguration())
            {
                // Signal the tray app to reload
                SignalTrayAppReload();
                
                MessageBox.Show("Configuration saved and applied successfully!\n\nThe tray application is reloading now.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void SignalTrayAppReload()
        {
            try
            {
                // Signal the tray app using a named event
                using (var reloadEvent = new System.Threading.EventWaitHandle(false, 
                    System.Threading.EventResetMode.AutoReset, 
                    "HPButtonRemap_ReloadConfig"))
                {
                    reloadEvent.Set(); // Signal the event
                }
            }
            catch
            {
                // Silently fail if we can't signal (tray app might not be running)
            }
        }
        
        private bool SaveConfiguration()
        {
            try
            {
                var config = new Config 
                { 
                    ButtonActions = new List<ButtonAction>(_actions),
                    ShowStartupNotification = _showStartupNotification
                };
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);

                // Try to save to Program Files, fall back to Local AppData
                string targetPath = ConfigPath;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                    File.WriteAllText(ConfigPath, json);
                }
                catch
                {
                    targetPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "HPButtonRemap",
                        "config.json"
                    );
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    File.WriteAllText(targetPath, json);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void ShowStartupNotificationCheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            _showStartupNotification = ShowStartupNotificationCheckBox.IsChecked ?? true;
        }

        private void ActionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ActionsListBox.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ActionEditorWindow();
            if (editor.ShowDialog() == true && editor.Action != null)
            {
                _actions.Add(editor.Action);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActionsListBox.SelectedItem is ButtonAction action)
            {
                var editor = new ActionEditorWindow(action);
                if (editor.ShowDialog() == true && editor.Action != null)
                {
                    int index = _actions.IndexOf(action);
                    _actions[index] = editor.Action;
                    ActionsListBox.Items.Refresh();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActionsListBox.SelectedItem is ButtonAction action)
            {
                if (MessageBox.Show($"Delete action '{action.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _actions.Remove(action);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}