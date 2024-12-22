using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public class CalendarSettings
    {
        public string? CalendarId { get; set; } = "primary";
        public int CheckIntervalSeconds { get; set; } = 60;
        public int LookAheadMinutes { get; set; } = 15;
    }
}
