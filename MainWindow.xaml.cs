using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Threading;

namespace GoogleCalendarNotifier
{
    public partial class MainWindow : MetroWindow
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly ConfigManager _configManager;
        private readonly INotificationService _notificationService;
        private readonly EventTrackingService _eventTrackingService;
        private ObservableCollection<CalendarEvent> _allEvents;
        private Dictionary<DateTime, List<CalendarEvent>> _dateEvents;
        private const string APP_NAME = "GoogleCalendarNotifier";
        private const string RUN_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private bool _suppressSelectionChange = false;

        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, 
                         ConfigManager configManager, INotificationService notificationService,
                         EventTrackingService eventTrackingService)
        {
            InitializeComponent();
            Debug.WriteLine("MainWindow: Constructor");

            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;
            _notificationService = notificationService;
            _eventTrackingService = eventTrackingService;
            _dateEvents = new Dictionary<DateTime, List<CalendarEvent>>();
            _allEvents = new ObservableCollection<CalendarEvent>();

            // For testing, set up some fake events
            SetupTestData();

            // Set up debug notification
            SetupDebugNotification();

            // Subscribe to snooze updates
            _eventTrackingService.SubscribeToSnoozeUpdates((sender, eventId) =>
            {
                var evt = _allEvents.FirstOrDefault(e => e.Id == eventId);
                if (evt != null)
                {
                    evt.SnoozeUntil = _eventTrackingService.GetSnoozeTime(eventId);
                }
            });
        }

        private void ClearSnooze_Click(object sender, RoutedEventArgs e)
        {
            if (EventsListView.SelectedItem is CalendarEvent selectedEvent)
            {
                Debug.WriteLine($"Clearing snooze for event: {selectedEvent.Title}");
                _eventTrackingService.ClearSnoozeTime(selectedEvent.Id);
                selectedEvent.SnoozeUntil = null;
            }
        }

        private void SetupDebugNotification()
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(10);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _notificationService.ShowNotification(
                    "Debug Event",
                    "This is a test notification that appears 10 seconds after startup.\n\n" +
                    "Start Time: " + DateTime.Now.ToString("g") + "\n" +
                    "Location: Test Location\n" +
                    "Description: This is a test event description.",
                    NotificationType.Success);
            };
            timer.Start();
        }

        private void SetupTestData()
        {
            Debug.WriteLine("MainWindow: Setting up test data");
            
            // Create some test events
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var nextWeek = today.AddDays(7);

            _allEvents.Clear();
            _allEvents.Add(new CalendarEvent 
            { 
                Id = "event1",
                Title = "Test Event 1", 
                StartTime = today.AddHours(14),
                EndTime = today.AddHours(15)
            });
            _allEvents.Add(new CalendarEvent 
            { 
                Id = "event2",
                Title = "Test Event 2",
                StartTime = tomorrow.AddHours(10),
                EndTime = tomorrow.AddHours(11)
            });
            _allEvents.Add(new CalendarEvent 
            { 
                Id = "event3",
                Title = "Test Event 3",
                StartTime = nextWeek.AddHours(9),
                EndTime = nextWeek.AddHours(10)
            });

            // Group events by date
            _dateEvents = _allEvents.GroupBy(e => e.StartTime.Date)
                                  .ToDictionary(g => g.Key, g => g.ToList());

            // Update calendar with test data
            MainCalendar.SetDatesWithEvents(_dateEvents.Keys);
            EventsListView.ItemsSource = _allEvents;
        }

        private void OnCalendarSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"MainWindow: Calendar selection changed");
            if (MainCalendar.SelectedDate.HasValue)
            {
                var selectedDate = MainCalendar.SelectedDate.Value;
                Debug.WriteLine($"  Selected date: {selectedDate:d}");
                HighlightEventsByDate(selectedDate);

                // Find events for the selected date
                if (_dateEvents.TryGetValue(selectedDate.Date, out var dateEvents))
                {
                    // Select the first event for this date in the ListView
                    _suppressSelectionChange = true;
                    EventsListView.SelectedItems.Clear();
                    foreach (var evt in dateEvents)
                    {
                        EventsListView.SelectedItems.Add(evt);
                    }
                    _suppressSelectionChange = false;

                    // Scroll the first event into view
                    if (dateEvents.Any())
                    {
                        EventsListView.ScrollIntoView(dateEvents[0]);
                    }
                }
                else
                {
                    // No events on this date, clear selection
                    _suppressSelectionChange = true;
                    EventsListView.SelectedItems.Clear();
                    _suppressSelectionChange = false;
                    EventDetailsTextBox.Clear();
                }
            }
        }

        private void HighlightEventsByDate(DateTime date)
        {
            foreach (CalendarEvent evt in _allEvents)
            {
                evt.IsHighlighted = evt.StartTime.Date == date.Date;
            }
        }

        private void OnEventTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChange) return;

            if (EventsListView.SelectedItem is CalendarEvent selectedEvent)
            {
                // Set the display date to the month of the selected event
                MainCalendar.DisplayDate = selectedEvent.StartTime.Date;
                
                // Then set the selected date (this will also highlight it)
                _suppressSelectionChange = true;
                MainCalendar.SelectedDate = selectedEvent.StartTime.Date;
                _suppressSelectionChange = false;
                
                HighlightEventsByDate(selectedEvent.StartTime.Date);

                var snoozeInfo = selectedEvent.SnoozeUntil.HasValue 
                    ? $"\nSnoozed until: {selectedEvent.SnoozeUntil.Value:MM/dd/yyyy HH:mm}"
                    : "";

                EventDetailsTextBox.Text = $"Event: {selectedEvent.Title}\n\n" +
                    $"Start: {selectedEvent.StartTime:MM/dd/yyyy HH:mm}\n" +
                    $"End: {selectedEvent.EndTime:MM/dd/yyyy HH:mm}\n" +
                    $"All Day: {selectedEvent.IsAllDay}\n" +
                    (selectedEvent.ReminderTime.HasValue ? $"Reminder: {selectedEvent.ReminderTime.Value} before start\n" : "") +
                    snoozeInfo +
                    (!string.IsNullOrEmpty(selectedEvent.Description) ? $"\nDescription:\n{selectedEvent.Description}" : "");
            }
            else if (EventsListView.SelectedItems.Count == 0)
            {
                EventDetailsTextBox.Clear();
            }
        }
    }
}