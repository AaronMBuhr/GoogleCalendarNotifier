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
        private bool _showHolidays = true; // Default to showing holidays
        
        // Reference UI elements - use FindName to get references at runtime
        private Button _refreshButton;
        private CustomCalendar _mainCalendar;
        private ListView _eventsListView;
        private TextBox _eventDetailsTextBox;
        private CheckBox _showHolidaysCheckBox;
        
        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, 
                         ConfigManager configManager, INotificationService notificationService,
                         EventTrackingService eventTrackingService)
        {
            // In a compiled WPF app, InitializeComponent would be auto-generated
            // Adding a dummy method here for design-time compatibility
            try 
            { 
                // Check if the method exists using reflection
                var method = this.GetType().GetMethod("InitializeComponent", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            } 
            catch { /* Ignore - component will be initialized at runtime */ }
            
            Debug.WriteLine("MainWindow: Constructor");

            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;
            _notificationService = notificationService;
            _eventTrackingService = eventTrackingService;
            _dateEvents = new Dictionary<DateTime, List<CalendarEvent>>();
            _allEvents = new ObservableCollection<CalendarEvent>();
            
            // Load settings
            _showHolidays = _configManager?.GetShowHolidays() ?? true;

            // Initialize UI control references
            InitializeUIControls();

            // Initialize real calendar data instead of test data
            // SetupTestData();  // Commented out test data
            
            // Load calendar data with feedback immediately on startup
            Dispatcher.BeginInvoke(new Action(async () => 
            {
                await LoadCalendarDataWithFeedbackAsync();
            }));

            // Set up debug notification
            // SetupDebugNotification();  // Commented out debug notification

            // Subscribe to snooze updates
            _eventTrackingService.SubscribeToSnoozeUpdates((sender, eventId) =>
            {
                Debug.WriteLine($"Snooze update received for eventId: {eventId}");
                var evt = _allEvents.FirstOrDefault(e => e.Id == eventId);
                if (evt != null)
                {
                    evt.SnoozeUntil = _eventTrackingService.GetSnoozeTime(eventId);
                    Debug.WriteLine($"Updated event {evt.Title} snooze time to {evt.SnoozeUntil}");
                }
                else
                {
                    Debug.WriteLine("Event not found!");
                }
            });
        }

        private void InitializeUIControls()
        {
            // Get references to UI controls
            _refreshButton = FindName("RefreshButton") as Button;
            _mainCalendar = FindName("MainCalendar") as CustomCalendar;
            _eventsListView = FindName("EventsListView") as ListView;
            _eventDetailsTextBox = FindName("EventDetailsTextBox") as TextBox;
            _showHolidaysCheckBox = FindName("ShowHolidaysCheckBox") as CheckBox;

            // Hook up events
            if (_refreshButton != null)
            {
                _refreshButton.Click += RefreshCalendar_Click;
            }
            
            // Set the initial state for the holidays checkbox
            if (_showHolidaysCheckBox != null)
            {
                _showHolidaysCheckBox.IsChecked = _showHolidays;
            }
        }

        private async void RefreshCalendar_Click(object sender, RoutedEventArgs e)
        {
            await LoadCalendarDataWithFeedbackAsync();
        }

        private async Task LoadCalendarDataWithFeedbackAsync()
        {
            if (_refreshButton == null) return;
            
            try
            {
                // Show loading indicator
                Debug.WriteLine("Loading real calendar data...");
                _refreshButton.IsEnabled = false;
                _refreshButton.Content = "Loading...";
                
                await LoadCalendarDataCoreAsync();
                
                _refreshButton.Content = "Refresh";
                _refreshButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in manual refresh: {ex.Message}");
                _refreshButton.Content = "Refresh";
                _refreshButton.IsEnabled = true;
            }
        }
        
        private async Task LoadCalendarDataCoreAsync()
        {
            try
            {
                // Initialize Google Calendar service
                await _calendarService.InitializeAsync();
                
                // Get events for the next 90 days (or whatever period is configured)
                var lookAheadTime = TimeSpan.FromDays(90);
                var events = await _calendarService.GetUpcomingEventsAsync(lookAheadTime, _showHolidays);
                
                _allEvents.Clear();
                foreach (var evt in events)
                {
                    // Apply any existing snooze settings from tracking service
                    evt.SnoozeUntil = _eventTrackingService.GetSnoozeTime(evt.Id);
                    _allEvents.Add(evt);
                }
                
                // Sort events chronologically
                var sortedEvents = _allEvents.OrderBy(e => e.StartTime.Date)
                                          .ThenBy(e => e.StartTime.TimeOfDay)
                                          .ToList();
                
                _allEvents.Clear();
                foreach (var evt in sortedEvents)
                {
                    _allEvents.Add(evt);
                }
                
                // Group events by date
                _dateEvents = _allEvents.GroupBy(e => e.StartTime.Date)
                                      .ToDictionary(g => g.Key, g => g.ToList());
                
                // Update calendar with real data
                if (_mainCalendar != null)
                {
                    _mainCalendar.SetDatesWithEvents(_dateEvents.Keys);
                    
                    // Set task dates separately
                    var taskDates = _allEvents
                        .Where(e => e.IsTask)
                        .Select(e => e.StartTime.Date)
                        .Distinct();
                    _mainCalendar.SetDatesWithTasks(taskDates);
                    
                    // Set holiday dates separately
                    var holidayDates = _allEvents
                        .Where(e => e.IsHoliday)
                        .Select(e => e.StartTime.Date)
                        .Distinct();
                    _mainCalendar.SetDatesWithHolidays(holidayDates);
                }
                
                if (_eventsListView != null)
                {
                    _eventsListView.ItemsSource = _allEvents;
                }
                
                Debug.WriteLine($"Successfully loaded {_allEvents.Count} events from Google Calendar");
                
                // Setup the monitoring service with real events
                _monitorService.SetEvents(_allEvents.ToList());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading calendar data: {ex.Message}");
                MessageBox.Show($"Error loading calendar data: {ex.Message}\n\nPlease check your Internet connection and Google Calendar credentials.", 
                               "Calendar Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fallback to test data if loading real data fails
                SetupTestData();
                throw; // Rethrow for UI handling
            }
        }

        private async void LoadCalendarDataAsync()
        {
            try
            {
                await LoadCalendarDataCoreAsync();
            }
            catch (Exception)
            {
                // Exception already handled in the Task method
            }
        }

        private void ClearSnooze_Click(object sender, RoutedEventArgs e)
        {
            if (_eventsListView?.SelectedItem is CalendarEvent selectedEvent)
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

                // Use the first event from our test data for debugging
                var testEvent = _allEvents.FirstOrDefault();
                if (testEvent != null)
                {
                    _notificationService.ShowNotification(
                        testEvent.Title,
                        $"This is a test notification that appears 10 seconds after startup.\n\n" +
                        $"Start Time: {testEvent.StartTime:g}\n" +
                        "Location: Test Location\n" +
                        "Description: This is a test event description.",
                        NotificationType.Success,
                        testEvent.Id);  // Pass the real event ID
                }
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

            // Sort events chronologically
            var sortedEvents = _allEvents.OrderBy(e => e.StartTime.Date)
                                      .ThenBy(e => e.StartTime.TimeOfDay)
                                      .ToList();
            
            _allEvents.Clear();
            foreach (var evt in sortedEvents)
            {
                _allEvents.Add(evt);
            }

            // Update calendar with test data
            if (_mainCalendar != null)
            {
                _mainCalendar.SetDatesWithEvents(_dateEvents.Keys);
                
                // Set task dates for test data
                var taskDates = _allEvents
                    .Where(e => e.IsTask)
                    .Select(e => e.StartTime.Date)
                    .Distinct();
                _mainCalendar.SetDatesWithTasks(taskDates);
                
                // Set holiday dates for test data
                var holidayDates = _allEvents
                    .Where(e => e.IsHoliday)
                    .Select(e => e.StartTime.Date)
                    .Distinct();
                _mainCalendar.SetDatesWithHolidays(holidayDates);
            }
            
            if (_eventsListView != null)
            {
                _eventsListView.ItemsSource = _allEvents;
            }
        }

        private void OnCalendarSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"MainWindow: Calendar selection changed");
            if (_mainCalendar?.SelectedDate.HasValue == true)
            {
                var selectedDate = _mainCalendar.SelectedDate.Value;
                Debug.WriteLine($"  Selected date: {selectedDate:d}");
                HighlightEventsByDate(selectedDate);

                // Find events for the selected date
                if (_dateEvents.TryGetValue(selectedDate.Date, out var dateEvents))
                {
                    // Select the first event for this date in the ListView
                    _suppressSelectionChange = true;
                    if (_eventsListView != null)
                    {
                        _eventsListView.SelectedItems.Clear();
                        foreach (var evt in dateEvents)
                        {
                            _eventsListView.SelectedItems.Add(evt);
                        }
                        
                        // Scroll the first event into view
                        if (dateEvents.Any())
                        {
                            _eventsListView.ScrollIntoView(dateEvents[0]);
                        }
                    }
                    _suppressSelectionChange = false;
                }
                else
                {
                    // No events on this date, clear selection
                    _suppressSelectionChange = true;
                    if (_eventsListView != null)
                    {
                        _eventsListView.SelectedItems.Clear();
                    }
                    _suppressSelectionChange = false;
                    
                    if (_eventDetailsTextBox != null)
                    {
                        _eventDetailsTextBox.Clear();
                    }
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

            if (_eventsListView?.SelectedItem is CalendarEvent selectedEvent)
            {
                // Set the display date to the month of the selected event
                if (_mainCalendar != null)
                {
                    _mainCalendar.DisplayDate = selectedEvent.StartTime.Date;
                    
                    // Then set the selected date (this will also highlight it)
                    _suppressSelectionChange = true;
                    _mainCalendar.SelectedDate = selectedEvent.StartTime.Date;
                    _suppressSelectionChange = false;
                }
                
                HighlightEventsByDate(selectedEvent.StartTime.Date);

                var snoozeInfo = selectedEvent.SnoozeUntil.HasValue 
                    ? $"\nSnoozed until: {selectedEvent.SnoozeUntil.Value:MM/dd/yyyy HH:mm}"
                    : "";

                if (_eventDetailsTextBox != null)
                {
                    _eventDetailsTextBox.Text = $"Event: {selectedEvent.Title}\n\n" +
                        $"Start: {selectedEvent.StartTime:MM/dd/yyyy HH:mm}\n" +
                        $"End: {selectedEvent.EndTime:MM/dd/yyyy HH:mm}\n" +
                        $"All Day: {selectedEvent.IsAllDay}\n" +
                        (selectedEvent.ReminderTime.HasValue ? $"Reminder: {selectedEvent.ReminderTime.Value} before start\n" : "") +
                        snoozeInfo +
                        (!string.IsNullOrEmpty(selectedEvent.Description) ? $"\nDescription:\n{selectedEvent.Description}" : "");
                }
            }
            else if (_eventsListView?.SelectedItems.Count == 0)
            {
                if (_eventDetailsTextBox != null)
                {
                    _eventDetailsTextBox.Clear();
                }
            }
        }

        private void ShowHolidays_Click(object sender, RoutedEventArgs e)
        {
            // Update the holidays display preference
            if (_showHolidaysCheckBox != null)
            {
                _showHolidays = _showHolidaysCheckBox.IsChecked ?? true;
                
                // Save preference to settings
                if (_configManager != null)
                {
                    try
                    {
                        _configManager.SaveSetting("ShowHolidays", _showHolidays);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving ShowHolidays setting: {ex.Message}");
                    }
                }
                
                // Refresh the calendar data with the new filter setting
                LoadCalendarDataAsync();
            }
        }
        
        private async void TaskCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is CalendarEvent calendarEvent)
            {
                // Make sure this is a task
                if (!calendarEvent.IsTask)
                {
                    Debug.WriteLine("TaskCheckbox_Click was called on a non-task event");
                    return;
                }
                
                Debug.WriteLine($"Task completion toggled for: {calendarEvent.Title}, IsCompleted: {checkBox.IsChecked}");
                
                // Update the task in Google Tasks if it's checked
                if (checkBox.IsChecked == true)
                {
                    try
                    {
                        // The TaskId is stored in the Id property, and the TaskListId is in the CalendarId property
                        var success = await _calendarService.CompleteTaskAsync(calendarEvent.Id, calendarEvent.CalendarId);
                        
                        if (success)
                        {
                            // Update the task status in the UI
                            calendarEvent.IsCompleted = true;
                            
                            // Show a toast notification that the task was completed
                            _notificationService.ShowNotification(
                                "Task Completed",
                                $"Task \"{calendarEvent.Title}\" has been marked as completed!",
                                NotificationType.Success);
                            
                            Debug.WriteLine($"Successfully completed task: {calendarEvent.Title}");
                        }
                        else
                        {
                            // If failed, revert the checkbox
                            checkBox.IsChecked = false;
                            calendarEvent.IsCompleted = false;
                            
                            // Show an error message
                            MessageBox.Show(
                                $"Failed to mark task \"{calendarEvent.Title}\" as completed. Please try again.",
                                "Task Completion Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            
                            Debug.WriteLine($"Failed to complete task: {calendarEvent.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // If an exception occurred, revert the checkbox
                        checkBox.IsChecked = false;
                        calendarEvent.IsCompleted = false;
                        
                        // Show an error message
                        MessageBox.Show(
                            $"An error occurred while completing the task: {ex.Message}",
                            "Task Completion Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                        Debug.WriteLine($"Exception completing task: {ex.Message}");
                    }
                }
                else
                {
                    // Just update the UI since we don't support uncompleting tasks
                    // (Google Tasks doesn't have a good way to "uncomplete" tasks)
                    calendarEvent.IsCompleted = false;
                    Debug.WriteLine($"Task marked as not completed in UI: {calendarEvent.Title}");
                    
                    // Show a message that uncompleting tasks is not supported
                    MessageBox.Show(
                        "Unmarking tasks as completed is not supported. " +
                        "You can create a new task if needed.",
                        "Unsupported Operation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    // Set the checkbox back to checked since we don't support uncompleting
                    if (calendarEvent.IsCompleted)
                    {
                        checkBox.IsChecked = true;
                    }
                }
            }
        }
    }
}