using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public class EventTrackingService
    {
        private HashSet<string> _notifiedEvents = new();

        public bool IsEventNotified(string eventId)
        {
            return _notifiedEvents.Contains(eventId);
        }
    }
}
