using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using System.Diagnostics;

namespace GoogleCalendarNotifier
{
    public partial class MainWindow : MetroWindow
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly ConfigManager _configManager;
        private List<CalendarEvent> _allEvents;
        private Dictionary<DateTime, List<CalendarEvent>> _dateEvents;
        private const string APP_NAME = "GoogleCalendarNotifier";
        private const string RUN_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private bool _suppressSelectionChange = false;

        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, ConfigManager configManager)
        {
            InitializeComponent();
            Debug.WriteLine("MainWindow: Constructor");

            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;
            _dateEvents = new Dictionary<DateTime, List<CalendarEvent>>();
            _allEvents = new List<CalendarEvent>();

            // For testing, set up some fake events
            SetupTestData();
        }

        private void SetupTestData()
        {
            Debug.WriteLine("MainWindow: Setting up test data");
            
            // Create some test events
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var nextWeek = today.AddDays(7);

            _allEvents = new List<CalendarEvent>
            {
                new CalendarEvent 
                { 
                    Title = "Test Event 1", 
                    StartTime = today.AddHours(14),
                    EndTime = today.AddHours(15)
                },
                new CalendarEvent 
                { 
                    Title = "Test Event 2",
                    StartTime = tomorrow.AddHours(10),
                    EndTime = tomorrow.AddHours(11)
                },
                new CalendarEvent 
                { 
                    Title = "Test Event 3",
                    StartTime = nextWeek.AddHours(9),
                    EndTime = nextWeek.AddHours(10)
                }
            };

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

            // Refresh the ListView to show highlighting
            if (EventsListView.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(EventsListView.ItemsSource).Refresh();
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

                EventDetailsTextBox.Text = $"Event: {selectedEvent.Title}\n\n" +
                    $"Start: {selectedEvent.StartTime:MM/dd/yyyy HH:mm}\n" +
                    $"End: {selectedEvent.EndTime:MM/dd/yyyy HH:mm}\n" +
                    $"All Day: {selectedEvent.IsAllDay}\n" +
                    (selectedEvent.ReminderTime.HasValue ? $"Reminder: {selectedEvent.ReminderTime.Value} before start\n" : "") +
                    (!string.IsNullOrEmpty(selectedEvent.Description) ? $"\nDescription:\n{selectedEvent.Description}" : "");
            }
            else if (EventsListView.SelectedItems.Count == 0)
            {
                EventDetailsTextBox.Clear();
            }
        }
    }
}