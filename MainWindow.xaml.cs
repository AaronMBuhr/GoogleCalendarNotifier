using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
// Fully qualify WPF controls to avoid ambiguity 
using WPFButton = System.Windows.Controls.Button;
using WPFListView = System.Windows.Controls.ListView;
using WPFTextBox = System.Windows.Controls.TextBox;
using WPFCheckBox = System.Windows.Controls.CheckBox;
using WPFContextMenu = System.Windows.Controls.ContextMenu;
using WPFMenuItem = System.Windows.Controls.MenuItem;
using WPFSeparator = System.Windows.Controls.Separator;
// End of fully qualified WPF controls
using System.Windows.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows.Threading;
// using H.NotifyIcon; // Removed
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Forms; // Added for NotifyIcon
using System.Windows.Controls; // Added for CalendarDateChangedEventArgs

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
        
        // Fields to track loaded date range
        private DateTime _earliestLoadedDate;
        private DateTime _latestLoadedDate;
        private bool _isInitialLoad = true;
        private DispatcherTimer _dayChangeTimer;
        private DateTime _lastKnownDate = DateTime.MinValue;
        
        // Reference UI elements - use FindName to get references at runtime
        private WPFButton _refreshButton;
        private CustomCalendar _mainCalendar;
        private WPFListView _eventsListView;
        private WPFTextBox _eventDetailsTextBox;
        private WPFCheckBox _showHolidaysCheckBox;
        // private TaskbarIcon? _notifyIcon; // Removed H.NotifyIcon
        private NotifyIcon? _tray; // WinForms NotifyIcon
        
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

            // Initialize and start the day change timer
            InitializeDayChangeTimer();

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

            // Add minimize-to-tray functionality
            Loaded += MainWindow_Loaded; // Changed from MainWindow_Loaded_SetupTrayIcon
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
        }

        // New method to set up the tray icon (replaces MainWindow_Loaded_SetupTrayIcon)
        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            System.Drawing.Icon? appIcon = null;
            try
            {
                var iconUri = new Uri("pack://application:,,,/app.ico", UriKind.RelativeOrAbsolute);
                System.Windows.Resources.StreamResourceInfo streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using (var iconStream = streamInfo.Stream)
                    {
                        appIcon = new System.Drawing.Icon(iconStream);
                    }
                }
                else
                {
                    Debug.WriteLine("Error loading app.ico: StreamResourceInfo is null. Using default icon.");
                    appIcon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading app.ico: {ex.Message}. Using default icon.");
                appIcon = SystemIcons.Application; // Fallback to default icon
            }

            _tray = new NotifyIcon
            {
                Icon = appIcon, // Use loaded app.ico or fallback
                Text = "Google Calendar Notifier",
                Visible = true,
                ContextMenuStrip = BuildContextMenu() // System.Windows.Forms.ContextMenuStrip
            };

            _tray.DoubleClick += (_, __) => RestoreFromTray();
        }

        private ContextMenuStrip BuildContextMenu() // System.Windows.Forms.ContextMenuStrip
        {
            var menu = new ContextMenuStrip(); // System.Windows.Forms.ContextMenuStrip

            var open = new ToolStripMenuItem("Open"); // System.Windows.Forms.ToolStripMenuItem
            open.Click += (_, __) => RestoreFromTray();
            menu.Items.Add(open);

            menu.Items.Add(new ToolStripSeparator()); // System.Windows.Forms.ToolStripSeparator

            var exit = new ToolStripMenuItem("Exit"); // System.Windows.Forms.ToolStripMenuItem
            exit.Click += (_, __) => System.Windows.Application.Current.Shutdown(); 
            menu.Items.Add(exit);

            return menu;
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
        
        // Minimize to tray instead of taskbar
        private void MainWindow_StateChanged(object? sender, EventArgs e) // Replaced method
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide(); // Corrected from this.Hide() for consistency
                Debug.WriteLine("Window minimized to tray.");
            }
        }

        // Handler for the custom minimize button in the title bar
        private void CustomMinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Removed NotifyIcon_TrayMouseDoubleClick method
        // Removed NotifyIcon_ExitClick method

        // Clean up icon on exit
        private void MainWindow_Closing(object? sender, CancelEventArgs e) // Replaced method
        {
            Debug.WriteLine("Window closing, disposing NotifyIcon.");
            _tray?.Dispose(); // Dispose the icon to remove it from the tray
            _tray = null; // Prevent further access
        }

        private void InitializeUIControls()
        {
            // Get references to UI controls
            _refreshButton = FindName("RefreshButton") as WPFButton;
            _mainCalendar = FindName("MainCalendar") as CustomCalendar;
            _eventsListView = FindName("EventsListView") as WPFListView;
            _eventDetailsTextBox = FindName("EventDetailsTextBox") as WPFTextBox;
            _showHolidaysCheckBox = FindName("ShowHolidaysCheckBox") as WPFCheckBox;

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
            
            // Subscribe to calendar display month changed
            if (_mainCalendar != null)
            {
                _mainCalendar.DisplayDateChanged += OnCalendarDisplayDateChanged;
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
                
                // Get the ExtentMonths setting from ConfigManager
                int extentMonths = _configManager.GetExtentMonths();
                
                // Calculate date range: from start of current month to ExtentMonths months ahead
                DateTime now = DateTime.Now;
                DateTime startDate = new DateTime(now.Year, now.Month, 1); // Start of current month
                DateTime endDate = startDate.AddMonths(extentMonths); // ExtentMonths from start of current month
                
                // Fetch events for the calculated date range
                var events = await _calendarService.GetEventsAsync(startDate, endDate, _showHolidays);
                
                // Store the loaded date range
                _earliestLoadedDate = startDate;
                _latestLoadedDate = endDate;
                _isInitialLoad = false;
                
                Debug.WriteLine($"Initial load date range: {_earliestLoadedDate:d} to {_latestLoadedDate:d}");
                
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
                System.Windows.MessageBox.Show($"Error loading calendar data: {ex.Message}\n\nPlease check your Internet connection and Google Calendar credentials.", 
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
            
            // Set initial date range for test data
            int extentMonths = _configManager?.GetExtentMonths() ?? 6;
            _earliestLoadedDate = new DateTime(today.Year, today.Month, 1);
            _latestLoadedDate = _earliestLoadedDate.AddMonths(extentMonths);
            _isInitialLoad = false;
            
            Debug.WriteLine($"Test data date range: {_earliestLoadedDate:d} to {_latestLoadedDate:d}");

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

        private void OnCalendarSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) // Explicitly System.Windows.Controls
        {
            Debug.WriteLine($"MainWindow: Calendar selection changed. Suppressed: {_suppressSelectionChange}");
            if (_suppressSelectionChange) return;

            if (_mainCalendar?.SelectedDate.HasValue == true)
            {
                var selectedDate = _mainCalendar.SelectedDate.Value;
                Debug.WriteLine($"  Selected date: {selectedDate:d}");
                HighlightEventsByDate(selectedDate);
                UpdateEventListForDate(selectedDate); // Use the new helper method
            }
        }

        private void HighlightEventsByDate(DateTime date)
        {
            foreach (CalendarEvent evt in _allEvents)
            {
                evt.IsHighlighted = evt.StartTime.Date == date.Date;
            }
        }

        private void OnEventTableSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) // Explicitly System.Windows.Controls
        {
            Debug.WriteLine("Event table selection changed");
            if (_suppressSelectionChange || _eventsListView == null || _eventDetailsTextBox == null) return;

            if (_eventsListView.SelectedItem is CalendarEvent selectedEvent)
            {
                Debug.WriteLine($"  Selected event: {selectedEvent.Title}");
                _eventDetailsTextBox.Text = selectedEvent.Description;

                // Update calendar selection
                if (_mainCalendar != null)
                {
                    _suppressSelectionChange = true; 
                    _mainCalendar.SelectedDate = selectedEvent.StartTime.Date;
                    _mainCalendar.DisplayDate = selectedEvent.StartTime.Date; // Also update DisplayDate to ensure the calendar view navigates if necessary
                    _suppressSelectionChange = false;
                }
            }
            else
            {
                _eventDetailsTextBox.Text = string.Empty;
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
        
        // When a task checkbox is clicked
        private async void TaskCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WPFCheckBox checkBox && checkBox.DataContext is CalendarEvent calendarEvent)
            {
                // Ensure IsTask is true before proceeding
                if (!calendarEvent.IsTask)
                {
                    Debug.WriteLine("TaskCheckbox_Click was called on a non-task event");
                    return;
                }

                Debug.WriteLine($"Task checkbox clicked for: {calendarEvent.Title}, IsCompleted from UI: {checkBox.IsChecked}");
                calendarEvent.IsCompleted = checkBox.IsChecked ?? false;

                if (calendarEvent.IsCompleted)
                {
                    Debug.WriteLine($"Attempting to complete task: {calendarEvent.Title}");
                    try
                    {
                        // The TaskId is stored in the Id property, and the TaskListId is in the CalendarId property
                        bool success = await _calendarService.CompleteTaskAsync(calendarEvent.Id, calendarEvent.CalendarId);
                        if (success)
                        {
                            Debug.WriteLine($"Successfully completed task: {calendarEvent.Title}");
                            // Optionally, show a success notification if desired
                            // _notificationService.ShowNotification("Task Completed", $"Task \"{calendarEvent.Title}\" marked as completed.", NotificationType.Success);
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to complete task via service: {calendarEvent.Title}");
                            // Revert UI change if service call failed
                            calendarEvent.IsCompleted = false;
                            checkBox.IsChecked = false;
                            System.Windows.MessageBox.Show($"Failed to mark task '{calendarEvent.Title}' as completed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error completing task: {ex.Message}");
                        calendarEvent.IsCompleted = false;
                        checkBox.IsChecked = false;
                        System.Windows.MessageBox.Show($"Error updating task: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Logic for when a task is 'unchecked' (marked as not completed)
                    // Currently, GoogleCalendarService.CompleteTaskAsync only completes tasks.
                    // If un-completing tasks needs to be fully supported, the service layer would need a corresponding method.
                    Debug.WriteLine($"Task marked as not completed in UI: {calendarEvent.Title}. Backend update for un-completing is not currently implemented via CompleteTaskAsync.");
                    // Potentially show a message that full un-completion via API is not implemented here
                    // System.Windows.MessageBox.Show("Marking tasks as not completed is a UI-only change for now.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void InitializeDayChangeTimer()
        {
            _dayChangeTimer = new DispatcherTimer();
            _dayChangeTimer.Interval = TimeSpan.FromSeconds(10); // Check every 10 seconds
            _dayChangeTimer.Tick += DayChangeTimer_Tick;
            _dayChangeTimer.Start();
            // Initial check to set the date correctly on startup
            DayChangeTimer_Tick(this, EventArgs.Empty);
        }

        private void DayChangeTimer_Tick(object sender, EventArgs e)
        {
            if (_lastKnownDate.Date != DateTime.Today)
            {
                Debug.WriteLine($"DayChangeTimer: Detected new day. Old: {_lastKnownDate.Date}, New: {DateTime.Today}");
                _lastKnownDate = DateTime.Today;

                if (_mainCalendar != null)
                {
                    // Suppress selection change events during programmatic update
                    bool originalSuppress = _suppressSelectionChange;
                    _suppressSelectionChange = true;
                    
                    _mainCalendar.DisplayDate = DateTime.Today;
                    _mainCalendar.SelectedDate = DateTime.Today; // This should trigger OnCalendarSelectionChanged if the date is different
                                                                // or if CustomCalendar internals ensure it.
                                                                // OnCalendarSelectionChanged will call HighlightEventsByDate and UpdateEventListForDate.
                    
                    _suppressSelectionChange = originalSuppress;

                    // If OnCalendarSelectionChanged didn't fire because selected date was already today
                    // (e.g. app started on this day), we might need to manually refresh.
                    // However, the logic in OnCalendarSelectionChanged *should* be called.
                    // To be safe, explicitly call them if the selection change is suppressed.
                    // Or, if _mainCalendar.SelectedDate was already DateTime.Today.
                    // The initial call to DayChangeTimer_Tick on startup should handle the first load.
                    // Subsequent calls to this method are for when the day *changes*.
                    // When SelectedDate is set, OnCalendarSelectionChanged will run.
                }
            }
        }
        
        // Helper method to consolidate logic from OnCalendarSelectionChanged for updating event list
        private void UpdateEventListForDate(DateTime date)
        {
            Debug.WriteLine($"UpdateEventListForDate: Updating event list for date: {date:d}");
            if (_eventsListView == null) return;

            // Ensure we operate on the Date part only for dictionary lookup
            DateTime dateOnly = date.Date;

            if (_dateEvents.TryGetValue(dateOnly, out var dateEvents))
            {
                bool originalSuppress = _suppressSelectionChange;
                _suppressSelectionChange = true;
                _eventsListView.SelectedItems.Clear();
                foreach (var evt in dateEvents)
                {
                    _eventsListView.SelectedItems.Add(evt);
                }
                
                if (dateEvents.Any())
                {
                    _eventsListView.ScrollIntoView(dateEvents[0]);
                }
                 _suppressSelectionChange = originalSuppress;
            }
            else
            {
                bool originalSuppress = _suppressSelectionChange;
                _suppressSelectionChange = true;
                _eventsListView.SelectedItems.Clear();
                _suppressSelectionChange = originalSuppress;
                
                if (_eventDetailsTextBox != null)
                {
                    _eventDetailsTextBox.Clear();
                }
            }
        }

        private void OnCalendarDisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            if (_isInitialLoad) return; // Skip during initial load
            
            Debug.WriteLine($"Calendar display date changed to: {e.AddedDate:d}");
            
            DateTime newDisplayDate = e.AddedDate.GetValueOrDefault().Date;
            
            // Get the first and last day of the displayed month
            DateTime firstDayOfMonth = new DateTime(newDisplayDate.Year, newDisplayDate.Month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            
            Debug.WriteLine($"First day of month: {firstDayOfMonth:d}, Last day of month: {lastDayOfMonth:d}");
            Debug.WriteLine($"Currently loaded range: {_earliestLoadedDate:d} to {_latestLoadedDate:d}");
            
            try
            {
                // If we've navigated forward to an unloaded month
                if (lastDayOfMonth > _latestLoadedDate)
                {
                    Debug.WriteLine("Navigated forward to unloaded month. Loading additional data...");
                    LoadAdditionalMonthsForward(firstDayOfMonth);
                }
                // If we've navigated backward to an unloaded month
                else if (firstDayOfMonth < _earliestLoadedDate)
                {
                    Debug.WriteLine("Navigated backward to unloaded month. Loading additional data...");
                    LoadAdditionalMonthsBackward(firstDayOfMonth);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading additional data: {ex.Message}");
                System.Windows.MessageBox.Show($"Error loading additional calendar data: {ex.Message}",
                    "Calendar Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task LoadAdditionalMonthsForward(DateTime startOfMonth)
        {
            if (_refreshButton == null) return;
            
            try
            {
                _refreshButton.IsEnabled = false;
                _refreshButton.Content = "Loading...";
                
                int extentMonths = _configManager.GetExtentMonths();
                DateTime endDate = startOfMonth.AddMonths(extentMonths - 1);
                
                Debug.WriteLine($"Loading forward from {startOfMonth:d} to {endDate:d}");
                
                // Load and merge new events
                await LoadAndMergeEvents(startOfMonth, endDate);
                
                // Update the latest loaded date
                _latestLoadedDate = endDate;
                
                _refreshButton.Content = "Refresh";
                _refreshButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _refreshButton.Content = "Refresh";
                _refreshButton.IsEnabled = true;
                throw; // Rethrow for outer handler
            }
        }
        
        private async Task LoadAdditionalMonthsBackward(DateTime endOfMonth)
        {
            if (_refreshButton == null) return;
            
            try
            {
                _refreshButton.IsEnabled = false;
                _refreshButton.Content = "Loading...";
                
                int extentMonths = _configManager.GetExtentMonths();
                DateTime startDate = endOfMonth.AddMonths(-(extentMonths - 1));
                
                Debug.WriteLine($"Loading backward from {startDate:d} to {endOfMonth:d}");
                
                // Load and merge new events
                await LoadAndMergeEvents(startDate, endOfMonth);
                
                // Update the earliest loaded date
                _earliestLoadedDate = startDate;
                
                _refreshButton.Content = "Refresh";
                _refreshButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _refreshButton.Content = "Refresh";
                _refreshButton.IsEnabled = true;
                throw; // Rethrow for outer handler
            }
        }
        
        private async Task LoadAndMergeEvents(DateTime startDate, DateTime endDate)
        {
            // Get events for the specified date range
            var newEvents = await _calendarService.GetEventsAsync(startDate, endDate, _showHolidays);
            
            // Apply any existing snooze settings
            foreach (var evt in newEvents)
            {
                evt.SnoozeUntil = _eventTrackingService.GetSnoozeTime(evt.Id);
            }
            
            // Merge with existing events (avoid duplicates)
            var existingEventIds = new HashSet<string>(_allEvents.Select(e => e.Id));
            foreach (var evt in newEvents)
            {
                if (!existingEventIds.Contains(evt.Id))
                {
                    _allEvents.Add(evt);
                }
            }
            
            Debug.WriteLine($"Added {newEvents.Count(e => !existingEventIds.Contains(e.Id))} new events");
            
            // Re-sort events
            var sortedEvents = _allEvents.OrderBy(e => e.StartTime.Date)
                                      .ThenBy(e => e.StartTime.TimeOfDay)
                                      .ToList();
            
            _allEvents.Clear();
            foreach (var evt in sortedEvents)
            {
                _allEvents.Add(evt);
            }
            
            // Update event grouping
            _dateEvents = _allEvents.GroupBy(e => e.StartTime.Date)
                                  .ToDictionary(g => g.Key, g => g.ToList());
            
            // Update calendar with new data
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
            
            // Update monitor service with the full event list
            _monitorService.SetEvents(_allEvents.ToList());
        }
    }
} 