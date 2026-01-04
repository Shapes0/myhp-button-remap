using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.ServiceProcess;
using Newtonsoft.Json;
using HPButtonRemap;

namespace HPButtonRemapConfig
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ButtonAction> _actions = new();
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = new Config { ButtonActions = new List<ButtonAction>(_actions) };
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

                MessageBox.Show($"Configuration saved successfully to:\n{targetPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var service = new ServiceController("HP Button Remap Service"))
                {
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        service.Start();
                        MessageBox.Show("Service restarted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        service.Start();
                        MessageBox.Show("Service started successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling service: {ex.Message}\n\nPlease restart manually from Services.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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