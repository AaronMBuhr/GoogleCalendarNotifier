using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public class CalendarMonitorService
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly INotificationService _notificationService;
        private readonly EventTrackingService _eventTracker;

        public CalendarMonitorService(
            IGoogleCalendarService calendarService,
            INotificationService notificationService,
            EventTrackingService eventTracker)
        {
            _calendarService = calendarService;
            _notificationService = notificationService;
            _eventTracker = eventTracker;
        }
    }
}
