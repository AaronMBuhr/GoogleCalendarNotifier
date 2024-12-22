using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCalendarNotifier.Notifications;

namespace GoogleCalendarNotifier
{
    public class NotificationSettings
    {
        public int NotificationLeadTime { get; set; }
        public bool EnableSound { get; set; }
        public NotificationStyle Style { get; set; }
    }
}
