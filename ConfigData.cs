using System;

namespace GoogleCalendarNotifier
{
    public class AppConfig
    {
        public PermanentConfig Settings { get; set; } = new();
        public StateData State { get; set; } = new();
    }

    public class PermanentConfig
    {
        // Default notification times that can be selected
        public TimeSpan[] DefaultNotificationTimes { get; set; } = new[]
        {
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromDays(1)
        };

        // Look-ahead time for checking upcoming events
        public TimeSpan LookAheadTime { get; set; } = TimeSpan.FromDays(7);
        
        // How often to check for events (in minutes)
        public int CheckIntervalMinutes { get; set; } = 5;
    }

    public class StateData
    {
        // Dictionary of event ID to snooze info
        public Dictionary<string, SnoozeInfo> SnoozedEvents { get; set; } = new();
        
        // Dictionary of event ID to custom notification settings
        public Dictionary<string, CustomNotification> CustomNotifications { get; set; } = new();
    }

    public class SnoozeInfo
    {
        // The event ID this snooze applies to
        public string EventId { get; set; } = "";
        
        // When this snooze expires
        public DateTime SnoozeUntil { get; set; }
        
        // Original notification time (so we can restore it after snooze)
        public DateTime OriginalNotificationTime { get; set; }
    }

    public class CustomNotification
    {
        // The event ID this notification is for
        public string EventId { get; set; } = "";
        
        // When to show the notification (absolute time)
        public DateTime NotificationTime { get; set; }
        
        // Optional: repeat interval
        public TimeSpan? RepeatInterval { get; set; }
        
        // Whether this notification is enabled
        public bool IsEnabled { get; set; } = true;
    }
}