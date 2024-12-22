using System.Windows;

namespace GoogleCalendarNotifier
{
    public partial class App : Application
    {
        private EventTrackingService? _eventTrackingService;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize services
            var calendarService = new GoogleCalendarService();
            var configManager = new ConfigManager();
            var notificationService = new WindowsNotificationService();
            _eventTrackingService = new EventTrackingService();
            var monitorService = new CalendarMonitorService(calendarService, notificationService, _eventTrackingService);

            // Create and show main window
            var mainWindow = new MainWindow(calendarService, monitorService, configManager);
            mainWindow.Show();
        }
    }
}