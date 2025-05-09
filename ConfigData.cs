using System;
using System.Collections.Generic;

namespace GoogleCalendarNotifier
{
    public class AppConfig
    {
        public PermanentConfig Settings { get; set; } = new();
        public StateData State { get; set; } = new();
    }

    public class PermanentConfig
    {
        public TimeSpan[] DefaultNotificationTimes { get; set; } = new[]
        {
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(1)
        };

        public TimeSpan LookAheadTime { get; set; } = TimeSpan.FromDays(7);
        public int CheckIntervalMinutes { get; set; } = 5;
        public bool ShowHolidays { get; set; } = true;
        public int ExtentMonths { get; set; } = 6;
    }

    public class StateData
    {
        public Dictionary<string, SnoozeInfo> SnoozedEvents { get; set; } = new();
        public Dictionary<string, CustomNotification> CustomNotifications { get; set; } = new();
    }

    public class CustomNotification
    {
        public string EventId { get; set; } = "";
        public DateTime NotificationTime { get; set; }
        public TimeSpan? RepeatInterval { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}

