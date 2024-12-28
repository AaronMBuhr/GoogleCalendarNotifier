
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
using System.Windows.Input;
using MahApps.Metro.Controls;
using System.Diagnostics;
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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
        }

        private async Task RefreshCalendarMonth()
        {
            // Implementation here...
        }

        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implementation for selected dates changed
        }

        private void MainCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            // Implementation for calendar loaded
        }

        private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implementation for events list selection changed
        }

        private void Snooze5Min_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for snooze 5 minutes
        }

        private void Snooze15Min_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for snooze 15 minutes
        }

        private void Snooze30Min_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for snooze 30 minutes
        }

        private void Snooze1Hour_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for snooze 1 hour
        }

        private void Snooze1Day_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for snooze 1 day
        }

        private void ClearNotification_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for clearing notifications
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
EOF

# Generate checksums for verification
echo "Generating checksums for $target_file..."
md5sum "$target_file"
wc "$target_file"

echo "Script execution completed. Please rebuild the project manually and provide the output."
