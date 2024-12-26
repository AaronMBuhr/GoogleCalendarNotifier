using System;

namespace GoogleCalendarNotifier
{
    public class SnoozeInfo
    {
        public required string EventId { get; set; }
        public DateTime UntilTime { get; set; }
        public DateTime OriginalNotificationTime { get; set; }
    }
}
