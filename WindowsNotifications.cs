using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier.Notifications
{
    public enum NotificationType
    {
        Information,
        Warning,
        Error,
        Success
    }

    public enum NotificationStyle
    {
        Balloon,
        Toast,
        Minimal
    }
}
