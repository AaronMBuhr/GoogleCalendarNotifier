using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Collections.Generic;
using Microsoft.Win32;

namespace GoogleCalendarNotifier
{
    public partial class MainWindow : Window
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly ConfigManager _configManager;
        private List<CalendarEvent> _allEvents;
        private Dictionary<DateTime, List<CalendarEvent>> _dateEvents;
        private const string APP_NAME = "GoogleCalendarNotifier";
        private const string RUN_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, ConfigManager configManager)
        {
            InitializeComponent();
            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;
            _dateEvents = new Dictionary<DateTime, List<CalendarEvent>>();
            _allEvents = new List<CalendarEvent>();
            
            MainCalendar.DisplayDateStart = DateTime.Today;
            MainCalendar.DisplayDateEnd = DateTime.Today.AddDays(90);
            
            InitializeAutoStartCheckbox();
            
            // Initial data load
            _ = RefreshEventsAsync();
        }

        private void InitializeAutoStartCheckbox()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION))
            {
                AutoStartCheckBox.IsChecked = key?.GetValue(APP_NAME) != null;
            }
            AutoStartCheckBox.Checked += OnAutoStartChanged;
            AutoStartCheckBox.Unchecked += OnAutoStartChanged;
        }

        private void OnAutoStartChanged(object sender, RoutedEventArgs e)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION, true))
            {
                if (AutoStartCheckBox.IsChecked == true)
                {
                    key?.SetValue(APP_NAME, System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                else
                {
                    key?.DeleteValue(APP_NAME, false);
                }
            }
        }

        private async Task RefreshEventsAsync()
        {
            try
            {
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(90);
                _allEvents = (await _calendarService.GetEventsAsync(startDate, endDate)).ToList();
                
                // Group events by date
                _dateEvents = _allEvents.GroupBy(e => e.StartTime.Date)
                                     .ToDictionary(g => g.Key, g => g.ToList());
                
                UpdateCalendarEventIndicators();
                EventsListView.ItemsSource = _allEvents;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing events: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCalendarEventIndicators()
        {
            foreach (CalendarDayButton dayButton in GetCalendarDayButtons())
            {
                if (dayButton.DataContext is DateTime date)
                {
                    var hasEvents = _dateEvents.ContainsKey(date.Date);
                    CalendarDayButtonExtensions.SetHasEvents(dayButton, hasEvents);
                }
            }
        }

        private IEnumerable<CalendarDayButton> GetCalendarDayButtons()
        {
            if (MainCalendar.Template == null) return Enumerable.Empty<CalendarDayButton>();
            var monthView = MainCalendar.Template.FindName("PART_CalendarView", MainCalendar) as Calendar;
            if (monthView == null) return Enumerable.Empty<CalendarDayButton>();
            
            return LogicalTreeHelper.GetChildren(monthView)
                .OfType<CalendarDayButton>();
        }

        private void HighlightEventsByDate(DateTime date)
        {
            foreach (CalendarEvent evt in _allEvents)
            {
                evt.IsHighlighted = evt.StartTime.Date == date.Date;
            }
        }

        private void OnCalendarSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                HighlightEventsByDate(MainCalendar.SelectedDate.Value);
            }
        }

        private void OnEventTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventsListView.SelectedItem is CalendarEvent selectedEvent)
            {
                MainCalendar.SelectedDate = selectedEvent.StartTime.Date;
                HighlightEventsByDate(selectedEvent.StartTime.Date);

                EventDetailsTextBox.Text = $"Event: {selectedEvent.Title}\n\n" +
                    $"Start: {selectedEvent.StartTime:MM/dd/yyyy HH:mm}\n" +
                    $"End: {selectedEvent.EndTime:MM/dd/yyyy HH:mm}\n" +
                    $"All Day: {selectedEvent.IsAllDay}\n" +
                    (selectedEvent.ReminderTime.HasValue ? $"Reminder: {selectedEvent.ReminderTime.Value} before start\n" : "") +
                    (!string.IsNullOrEmpty(selectedEvent.Description) ? $"\nDescription:\n{selectedEvent.Description}" : "");
            }
            else
            {
                EventDetailsTextBox.Clear();
            }
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await RefreshEventsAsync();
        }
    }
}