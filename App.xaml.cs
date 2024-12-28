using System;
using System.Windows;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public partial class App : Application
    {
        private EventTrackingService? _eventTrackingService;
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize services
                var calendarService = new GoogleCalendarService();
                await calendarService.InitializeAsync();  // Now properly initializing the service
                
                var configManager = new ConfigManager();
                var notificationService = new WindowsNotificationService();
                _eventTrackingService = new EventTrackingService();
                var monitorService = new CalendarMonitorService(calendarService, notificationService, _eventTrackingService);

                // Create and show main window
                var mainWindow = new MainWindow(calendarService, monitorService, configManager);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}