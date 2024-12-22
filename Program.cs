using GoogleCalendarNotifier.Notifications;

namespace GoogleCalendarNotifier
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var calendarService = new GoogleCalendarService();
            var notificationService = new WindowsNotificationService();
            var eventTracker = new EventTrackingService();
            var settingsManager = new SettingsManager();

            var monitorService = new CalendarMonitorService(
                calendarService,
                notificationService,
                eventTracker);

            Application.Run(new MainForm(calendarService, monitorService, settingsManager));
        }
    }
}