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
        private NotificationSettings _settings;

#if old
        public WindowsNotificationService()
        {
            _trayIcon = new TaskbarIcon();
            _settings = new NotificationSettings();
            
            // Initialize tray icon
            _trayIcon.Icon = System.Drawing.SystemIcons.Information;
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
                    _trayIcon.Icon = System.Drawing.SystemIcons.Warning;
                    break;
                case NotificationType.Error:
                    builder.AddAttributionText("Error");
                    _trayIcon.Icon = System.Drawing.SystemIcons.Error;
                    break;
                case NotificationType.Success:
                    builder.AddAttributionText("Success");
                    _trayIcon.Icon = System.Drawing.SystemIcons.Information;
                    break;
                default:
                    _trayIcon.Icon = System.Drawing.SystemIcons.Information;
                    break;
            }

            builder.Show();

            // Also show in system tray
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.ShowBalloonTip(3000);
        }

        void INotificationService.Configure(NotificationSettings settings)
        {
            _settings = settings;
        }

        ~WindowsNotificationService()
        {
            _trayIcon.Dispose();
        }

#endif

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
            _settings = settings;
        }

        ~WindowsNotificationService()
        {
            _trayIcon.Dispose();
        }

    }
}