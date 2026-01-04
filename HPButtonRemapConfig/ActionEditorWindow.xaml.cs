using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using HPButtonRemap;

namespace HPButtonRemapConfig
{
    public partial class ActionEditorWindow : Window
    {
        public ButtonAction? Action { get; private set; }

        public ActionEditorWindow(ButtonAction? existingAction = null)
        {
            InitializeComponent();

            if (existingAction != null)
            {
                // Edit mode
                NameTextBox.Text = existingAction.Name;
                EventIDTextBox.Text = existingAction.EventID.ToString();
                EventDataTextBox.Text = existingAction.EventData.ToString();

                // Select appropriate action type
                foreach (ComboBoxItem item in ActionTypeComboBox.Items)
                {
                    if (item.Tag.ToString() == existingAction.Type.ToString())
                    {
                        ActionTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Fill in type-specific fields
                switch (existingAction.Type)
                {
                    case ActionType.LaunchApp:
                        LaunchPathTextBox.Text = existingAction.LaunchPath ?? "";
                        LaunchArgsTextBox.Text = existingAction.LaunchArguments ?? "";
                        break;
                    case ActionType.OpenWebsite:
                        WebsiteUrlTextBox.Text = existingAction.WebsiteUrl ?? "";
                        break;
                    case ActionType.SendKeys:
                        KeyComboTextBox.Text = existingAction.KeyCombo ?? "";
                        break;
                }
            }
            else
            {
                // New action defaults
                EventIDTextBox.Text = "29";
                EventDataTextBox.Text = "8616";
                ActionTypeComboBox.SelectedIndex = 0;
            }
        }

        private void ActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionTypeComboBox.SelectedItem is ComboBoxItem selected)
            {
                // Hide all panels
                LaunchAppPanel.Visibility = Visibility.Collapsed;
                OpenWebsitePanel.Visibility = Visibility.Collapsed;
                SendKeysPanel.Visibility = Visibility.Collapsed;

                // Show selected panel
                switch (selected.Tag.ToString())
                {
                    case "LaunchApp":
                        LaunchAppPanel.Visibility = Visibility.Visible;
                        break;
                    case "OpenWebsite":
                        OpenWebsitePanel.Visibility = Visibility.Visible;
                        break;
                    case "SendKeys":
                        SendKeysPanel.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Application"
            };

            if (dialog.ShowDialog() == true)
            {
                LaunchPathTextBox.Text = dialog.FileName;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter an action name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(EventIDTextBox.Text, out int eventId))
            {
                MessageBox.Show("Event ID must be a number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(EventDataTextBox.Text, out int eventData))
            {
                MessageBox.Show("Event Data must be a number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ActionTypeComboBox.SelectedItem is not ComboBoxItem selected)
            {
                MessageBox.Show("Please select an action type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create action based on type
            ActionType actionType = selected.Tag.ToString() switch
            {
                "LaunchApp" => ActionType.LaunchApp,
                "OpenWebsite" => ActionType.OpenWebsite,
                "SendKeys" => ActionType.SendKeys,
                _ => ActionType.LaunchApp
            };

            Action = new ButtonAction
            {
                Name = NameTextBox.Text,
                EventID = eventId,
                EventData = eventData,
                Type = actionType
            };

            // Set type-specific fields
            switch (actionType)
            {
                case ActionType.LaunchApp:
                    if (string.IsNullOrWhiteSpace(LaunchPathTextBox.Text))
                    {
                        MessageBox.Show("Please enter an application path.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    Action.LaunchPath = LaunchPathTextBox.Text;
                    Action.LaunchArguments = LaunchArgsTextBox.Text;
                    break;

                case ActionType.OpenWebsite:
                    if (string.IsNullOrWhiteSpace(WebsiteUrlTextBox.Text))
                    {
                        MessageBox.Show("Please enter a website URL.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    Action.WebsiteUrl = WebsiteUrlTextBox.Text;
                    break;

                case ActionType.SendKeys:
                    if (string.IsNullOrWhiteSpace(KeyComboTextBox.Text))
                    {
                        MessageBox.Show("Please enter a key combination.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    Action.KeyCombo = KeyComboTextBox.Text;
                    break;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}