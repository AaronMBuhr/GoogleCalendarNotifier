using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace GoogleCalendarNotifier
{
    public class WindowsNotificationService : INotificationService
    {
        private readonly NotifyIcon _trayIcon;
        private NotificationSettings _settings;

        public WindowsNotificationService()
        {
            _trayIcon = new NotifyIcon();
            _settings = new NotificationSettings();
            
            // Initialize tray icon
            _trayIcon.Icon = System.Drawing.SystemIcons.Information;
            _trayIcon.Visible = true;
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
    }
}