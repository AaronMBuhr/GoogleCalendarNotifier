using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCalendarNotifier.Notifications;
// using H.NotifyIcon; // Removed

namespace GoogleCalendarNotifier
{
    public interface INotificationService
    {
        void ShowNotification(string title, string message, NotificationType type);
        void ShowNotification(string title, string message, NotificationType type, string eventId);
        void Configure(NotificationSettings settings);
        // void SetTrayIcon(H.NotifyIcon.TaskbarIcon icon); // Commented out as TaskbarIcon is removed and NotifyIcon is managed in MainWindow
    }
}
