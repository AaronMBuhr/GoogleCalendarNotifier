using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier.Notifications
{
    public class WindowsNotificationService : INotificationService
    {
        private readonly NotifyIcon _trayIcon;
        private NotificationSettings _settings;

        public WindowsNotificationService()
        {
            _trayIcon = new NotifyIcon();
            _settings = new NotificationSettings();
        }

        void INotificationService.ShowNotification(string title, string message, NotificationType type)
        {
            var icon = type switch
            {
                NotificationType.Error => ToolTipIcon.Error,
                NotificationType.Warning => ToolTipIcon.Warning,
                _ => ToolTipIcon.Info
            };

            _trayIcon.ShowBalloonTip(3000, title, message, icon);
        }

        void INotificationService.Configure(NotificationSettings settings)
        {
            _settings = settings;
        }
    }
}
