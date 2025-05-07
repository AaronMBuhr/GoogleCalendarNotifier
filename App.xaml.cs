using System;
using System.Windows;
using System.Diagnostics;
// using H.NotifyIcon; // Removed

namespace GoogleCalendarNotifier
{
    public partial class App : System.Windows.Application // Fully qualify to resolve ambiguity
    {
        private IGoogleCalendarService _calendarService;
        private CalendarMonitorService _monitorService;
        private ConfigManager _configManager;
        private EventTrackingService _eventTrackingService;
        private WindowsNotificationService _notificationService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize services
                _configManager = new ConfigManager();
                _calendarService = new GoogleCalendarService();
                _eventTrackingService = new EventTrackingService();
                
                // Create notification service
                _notificationService = new WindowsNotificationService(_eventTrackingService);
                
                // Create monitor service after notification service is created
                _monitorService = new CalendarMonitorService(_calendarService, _notificationService, _eventTrackingService);
                
                // Create and show main window
                var mainWindow = new MainWindow(_calendarService, _monitorService, _configManager, _notificationService, _eventTrackingService);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during startup: {ex}");
                System.Windows.MessageBox.Show($"Error during startup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(); // Use Current to access Shutdown
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}