
#!/bin/bash

# Define the target file
target_file="./MainWindow.xaml.cs"

# Backup the original file
if [[ -f "$target_file" ]]; then
  echo "Backing up the original $target_file..."
  cp "$target_file" "${target_file}.bak"
fi

# Replace the content of MainWindow.xaml.cs
echo "Replacing $target_file with updated content..."
cat > "$target_file" << 'EOF'
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Text;
using H.NotifyIcon;

namespace GoogleCalendarNotifier
{
    public partial class MainWindow : MetroWindow
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly ConfigManager _configManager;
        private TaskbarIcon? _notifyIcon;

        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, ConfigManager configManager)
        {
            InitializeComponent();
            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;
            _notifyIcon = this.FindName("NotifyIcon") as TaskbarIcon;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _calendarService.InitializeAsync();
                MainCalendar.SelectedDate = DateTime.Today;
                await RefreshCalendarMonth();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MainWindow_Loaded: {ex}");
                MessageBox.Show($"Error initializing calendar service: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshCalendarMonth()
        {
            // Implementation here...
        }

        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _notifyIcon?.Dispose();
            Close();
        }
    }
}
EOF

# Generate checksums for verification
echo "Generating checksums for $target_file..."
md5sum "$target_file"
wc "$target_file"

echo "Script execution completed. Please rebuild the project manually and provide the output."
