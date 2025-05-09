using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;

namespace GoogleCalendarNotifier
{
    public class CalendarMonitorService
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly INotificationService _notificationService;
        private readonly EventTrackingService _eventTracker;
        private readonly ConfigManager _configManager;
        private List<CalendarEvent> _events;
        private DispatcherTimer _monitorTimer;
        private DispatcherTimer _refreshTimer;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(5);

        public CalendarMonitorService(
            IGoogleCalendarService calendarService,
            INotificationService notificationService,
            EventTrackingService eventTracker,
            ConfigManager configManager)
        {
            _calendarService = calendarService;
            _notificationService = notificationService;
            _eventTracker = eventTracker;
            _configManager = configManager;
            _events = new List<CalendarEvent>();
            
            // Set up timer for checking events
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = _checkInterval;
            _monitorTimer.Tick += CheckForUpcomingEvents;
            _monitorTimer.Start();
            
            // Set up timer for refreshing event data
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = _refreshInterval;
            _refreshTimer.Tick += RefreshCalendarData;
        }
        
        public void SetEvents(List<CalendarEvent> events)
        {
            _events = events;
            _refreshTimer.Start(); // Start the refresh timer once we have events
            Debug.WriteLine($"CalendarMonitorService: Set {events.Count} events for monitoring");
        }
        
        private void CheckForUpcomingEvents(object sender, EventArgs e)
        {
            Debug.WriteLine("CalendarMonitorService: Checking for upcoming events");
            var now = DateTime.Now;
            
            foreach (var evt in _events)
            {
                // Skip events that have already been snoozed
                if (evt.SnoozeUntil.HasValue && evt.SnoozeUntil.Value > now)
                {
                    Debug.WriteLine($"Skipping snoozed event: {evt.Title}, snoozed until {evt.SnoozeUntil.Value}");
                    continue;
                }
                
                // Calculate notification time based on reminder or default time
                DateTime notificationTime;
                if (evt.ReminderTime.HasValue)
                {
                    notificationTime = evt.StartTime - evt.ReminderTime.Value;
                }
                else
                {
                    // Default reminder time (30 minutes)
                    notificationTime = evt.StartTime - TimeSpan.FromMinutes(30);
                }
                
                // Check if it's time to notify
                var timeDiff = notificationTime - now;
                if (timeDiff.TotalMinutes <= 1 && timeDiff.TotalMinutes >= -1)
                {
                    Debug.WriteLine($"Triggering notification for event: {evt.Title}");
                    
                    string message = $"Start Time: {evt.StartTime:g}\n";
                    if (!string.IsNullOrEmpty(evt.Description))
                    {
                        message += $"Description: {evt.Description}\n";
                    }
                    
                    _notificationService.ShowNotification(
                        evt.Title, 
                        message,
                        NotificationType.Success,
                        evt.Id);
                }
            }
        }
        
        private async void RefreshCalendarData(object sender, EventArgs e)
        {
            Debug.WriteLine("CalendarMonitorService: Refreshing calendar data");
            try
            {
                // Get the ExtentMonths setting
                int extentMonths = _configManager.GetExtentMonths();
                
                // Calculate date range: from start of current month to ExtentMonths months ahead
                DateTime now = DateTime.Now;
                DateTime startDate = new DateTime(now.Year, now.Month, 1); // Start of current month
                DateTime endDate = startDate.AddMonths(extentMonths); // ExtentMonths from start of current month
                
                // Fetch events for the calculated date range
                var events = await _calendarService.GetEventsAsync(startDate, endDate);
                
                // Preserve snooze settings from existing events
                var updatedEvents = events.ToList();
                foreach (var updatedEvent in updatedEvents)
                {
                    // Find corresponding event in current list
                    var existingEvent = _events.FirstOrDefault(e => e.Id == updatedEvent.Id);
                    if (existingEvent != null)
                    {
                        // Preserve snooze time from existing event
                        updatedEvent.SnoozeUntil = existingEvent.SnoozeUntil;
                    }
                    else
                    {
                        // For new events, check if there's a stored snooze time
                        updatedEvent.SnoozeUntil = _eventTracker.GetSnoozeTime(updatedEvent.Id);
                    }
                }
                
                _events = updatedEvents;
                Debug.WriteLine($"CalendarMonitorService: Refreshed calendar data, now tracking {_events.Count} events");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing calendar data: {ex.Message}");
                // Keep using existing events if refresh fails
            }
        }
    }
}
