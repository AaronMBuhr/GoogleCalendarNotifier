using System;

namespace GoogleCalendarNotifier
{
    public class CalendarEvent
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsAllDay { get; set; }
        public TimeSpan? ReminderTime { get; set; }
    }
}