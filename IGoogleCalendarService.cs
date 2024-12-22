using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Calendar.v3.Data;

namespace GoogleCalendarNotifier
{
    public interface IGoogleCalendarService
    {
        Task InitializeAsync();
        Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(TimeSpan lookAheadTime);
    }
}
