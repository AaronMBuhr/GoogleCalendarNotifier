using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public interface IGoogleCalendarService
    {
        System.Threading.Tasks.Task InitializeAsync();
        System.Threading.Tasks.Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate, bool includeHolidays = true);
        System.Threading.Tasks.Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(TimeSpan lookAheadTime, bool includeHolidays = true);
        System.Threading.Tasks.Task<bool> CompleteTaskAsync(string taskId, string taskListId);
    }
}