using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using H.NotifyIcon;
using Microsoft.Toolkit.Uwp.Notifications;

namespace GoogleCalendarNotifier
{
    public class WindowsNotificationService : INotificationService
    {
        private readonly TaskbarIcon _trayIcon;
        private readonly NotificationSettings _settings;

        public WindowsNotificationService()
        {
            _trayIcon = new TaskbarIcon();
            _settings = new NotificationSettings();
        }

        void INotificationService.ShowNotification(string title, string message, NotificationType type)
        {
            // Use Windows Toast Notifications
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(message);

            // Adjust the notification based on type
            switch (type)
            {
                case NotificationType.Warning:
                    builder.AddAttributionText("Warning");
                    _trayIcon.ToolTipText = "Warning: " + message;
                    break;
                case NotificationType.Error:
                    builder.AddAttributionText("Error");
                    _trayIcon.ToolTipText = "Error: " + message;
                    break;
                case NotificationType.Success:
                    builder.AddAttributionText("Success");
                    _trayIcon.ToolTipText = "Success: " + message;
                    break;
                default:
                    _trayIcon.ToolTipText = message;
                    break;
            }

            builder.Show();
        }

        void INotificationService.Configure(NotificationSettings settings)
        {
            // Update any configurable settings here
        }

        ~WindowsNotificationService()
        {
            _trayIcon.Dispose();
        }
    }
}
