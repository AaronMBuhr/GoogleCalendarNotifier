using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Collections.Generic;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using H.NotifyIcon;


namespace GoogleCalendarNotifier
{
    public partial class MainWindow : MetroWindow
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly ConfigManager _configManager;
        private IEnumerable<CalendarEvent> _monthEvents = new List<CalendarEvent>();
        private bool _isUpdatingSelection = false;
        private bool _isClosing = false;
        private TaskbarIcon? _notifyIcon;


        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, ConfigManager configManager)
        {
            InitializeComponent();
            
            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;
            _notifyIcon = this.FindName("TaskbarIcon") as TaskbarIcon;

            Loaded += MainWindow_Loaded;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _isClosing = true;
            _notifyIcon?.Dispose();
            Close();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _calendarService.InitializeAsync();
                MainCalendar.SelectedDate = DateTime.Today;
                await RefreshCalendarMonth();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing calendar service: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MainCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateCalendarDayButtons();
        }

        private async Task RefreshCalendarMonth()
        {
            try
            {
                var startDate = new DateTime(MainCalendar.DisplayDate.Year, MainCalendar.DisplayDate.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                _monthEvents = await _calendarService.GetEventsAsync(startDate, endDate);
                await UpdateCalendarDayButtons();
                await RefreshEventList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing calendar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateCalendarDayButtons()
        {
            if (_monthEvents == null) return;

            await Task.Run(() => {
                Dispatcher.Invoke(() => {
                    var calendar = MainCalendar.Template.FindName("PART_CalendarItem", MainCalendar) as CalendarItem;
                    if (calendar == null) return;

                    var monthView = calendar.Template.FindName("PART_MonthView", calendar) as Grid;
                    if (monthView == null) return;

                    foreach (var child in monthView.Children)
                    {
                        if (child is CalendarDayButton dayButton)
                        {
                            var date = dayButton.DataContext as DateTime?;
                            if (date.HasValue)
                            {
                                var hasEvents = _monthEvents.Any(e => e.StartTime.Date == date.Value.Date);
                                dayButton.DataContext = new CalendarDayData { HasEvents = hasEvents };
                            }
                        }
                    }
                });
            });
        }

        private async Task RefreshEventList()
        {
            if (!MainCalendar.SelectedDate.HasValue) return;

            var selectedDate = MainCalendar.SelectedDate.Value;
            var todaysEvents = _monthEvents.Where(e => e.StartTime.Date == selectedDate.Date)
                                         .OrderBy(e => e.StartTime);

            var items = todaysEvents.Select(evt =>
            {
                var snoozeInfo = _configManager.GetEventSnoozeInfo(evt.Id);
                string notificationTime = snoozeInfo != null ? 
                    $"Snoozed until {snoozeInfo.UntilTime:HH:mm}" : 
                    "Not snoozed";

                return new EventListItem
                {
                    DateTime = evt.StartTime.ToString("g"),
                    Title = evt.Title,
                    NotificationTime = notificationTime,
                    IsSelectedDay = true,
                    Event = evt
                };
            }).ToList();

            EventsList.ItemsSource = items;

            if (items.Any())
            {
                var details = new StringBuilder();
                var selectedEvent = items.FirstOrDefault()?.Event;
                if (selectedEvent != null)
                {
                    details.AppendLine($"Title: {selectedEvent.Title}");
                    details.AppendLine($"Start: {selectedEvent.StartTime:g}");
                    if (selectedEvent.EndTime != null)
                        details.AppendLine($"End: {selectedEvent.EndTime:g}");
                    if (!string.IsNullOrEmpty(selectedEvent.Description))
                        details.AppendLine($"\nDescription:\n{selectedEvent.Description}");
                }
                EventDetailsBlock.Text = details.ToString();
            }
            else
            {
                EventDetailsBlock.Text = "No events selected";
            }
        }

        private async void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;
            await RefreshEventList();
        }

        private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = EventsList.SelectedItem as EventListItem;
            if (item?.Event == null) return;

            var details = new StringBuilder();
            details.AppendLine($"Title: {item.Event.Title}");
            details.AppendLine($"Start: {item.Event.StartTime:g}");
            if (item.Event.EndTime != null)
                details.AppendLine($"End: {item.Event.EndTime:g}");
            if (!string.IsNullOrEmpty(item.Event.Description))
                details.AppendLine($"\nDescription:\n{item.Event.Description}");

            EventDetailsBlock.Text = details.ToString();
        }

        private async void Snooze5Min_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromMinutes(5));
        private async void Snooze15Min_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromMinutes(15));
        private async void Snooze30Min_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromMinutes(30));
        private async void Snooze1Hour_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromHours(1));
        private async void Snooze1Day_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromDays(1));

        private async void ClearNotification_Click(object sender, RoutedEventArgs e)
        {
            var item = EventsList.SelectedItem as EventListItem;
            if (item?.Event is not CalendarEvent evt || evt.Id == null) 
                return;

            _configManager.ClearEventSnooze(evt.Id);
            await RefreshEventList();
        }

        private async Task SnoozeEvent(TimeSpan duration)
        {
            var item = EventsList.SelectedItem as EventListItem;
            if (item?.Event is not CalendarEvent evt || evt.Id == null) 
                return;

            var until = DateTime.Now + duration;
            _configManager.SnoozeEvent(evt.Id, until, evt.StartTime);
            await RefreshEventList();
        }
    }

    public class EventListItem
    {
        public required string DateTime { get; set; }
        public required string Title { get; set; }
        public required string NotificationTime { get; set; }
        public bool IsSelectedDay { get; set; }
        public required CalendarEvent Event { get; set; }
    }
}
