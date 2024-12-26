using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public interface IGoogleCalendarService
    {
        Task InitializeAsync();
        Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(TimeSpan lookAheadTime);
    }
}