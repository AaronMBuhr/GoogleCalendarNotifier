using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCalendarNotifier.Notifications;

namespace GoogleCalendarNotifier
{
    public interface INotificationService
    {
        void ShowNotification(string title, string message, NotificationType type);
        void Configure(NotificationSettings settings);
    }
}
