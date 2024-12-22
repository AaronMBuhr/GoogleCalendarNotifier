using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public class CalendarEvent
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }  // Added this
        public bool IsAllDay { get; set; }
        public TimeSpan? ReminderTime { get; set; }
    }
}
