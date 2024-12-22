using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace GoogleCalendarNotifier
{
    public partial class MainWindow : MetroWindow
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly ConfigManager _configManager;

        public MainWindow(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, ConfigManager configManager)
        {
            InitializeComponent();
            
            _calendarService = calendarService;
            _monitorService = monitorService;
            _configManager = configManager;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _calendarService.InitializeAsync();
                MainCalendar.SelectedDate = DateTime.Today;
                await RefreshEventList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing calendar service: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private async void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshEventList();
        }

        private async Task RefreshEventList()
        {
            EventsList.Items.Clear();

            try
            {
                var date = MainCalendar.SelectedDate ?? DateTime.Today;
                var events = await _calendarService.GetUpcomingEventsAsync(TimeSpan.FromDays(1));
                var dayEvents = events.Where(e => e.StartTime.Date == date.Date);

                foreach (var evt in dayEvents)
                {
                    if (evt.Id == null) continue;

                    var item = new
                    {
                        Time = evt.IsAllDay ? "All Day" : evt.StartTime.ToShortTimeString(),
                        Title = evt.Title ?? "(No Title)",
                        NotificationTime = GetNotificationDisplayText(evt),
                        Event = evt
                    };

                    EventsList.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading events: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetNotificationDisplayText(CalendarEvent evt)
        {
            if (evt.Id == null) return "Default";

            var customNotif = _configManager.GetCustomNotification(evt.Id);
            if (customNotif != null)
                return customNotif.NotificationTime.ToShortTimeString();

            if (_configManager.IsEventSnoozed(evt.Id, out DateTime snoozeUntil))
                return $"Snoozed until {snoozeUntil:t}";

            return "Default";
        }

        private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = EventsList.SelectedItem as dynamic;
            if (selectedItem?.Event is CalendarEvent evt && evt.Id != null)
            {
                var customNotif = _configManager.GetCustomNotification(evt.Id);

                NotificationCalendar.IsEnabled = true;
                NotificationCalendar.SelectedDate = customNotif?.NotificationTime ?? evt.StartTime;
            }
            else
            {
                NotificationCalendar.IsEnabled = false;
                NotificationCalendar.SelectedDate = null;
            }
        }

        private async void SetCustomNotification_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = EventsList.SelectedItem as dynamic;
            if (selectedItem?.Event is not CalendarEvent evt || evt.Id == null) 
                return;

            var notifDate = NotificationCalendar.SelectedDate;
            if (!notifDate.HasValue) return;

            _configManager.SetCustomNotification(evt.Id, notifDate.Value);
            await RefreshEventList();
        }

        private async void ClearNotification_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = EventsList.SelectedItem as dynamic;
            if (selectedItem?.Event is not CalendarEvent evt || evt.Id == null) 
                return;

            _configManager.RemoveCustomNotification(evt.Id);
            _configManager.ClearSnooze(evt.Id);
            await RefreshEventList();
        }

        private async void Snooze5Min_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromMinutes(5));
        private async void Snooze15Min_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromMinutes(15));
        private async void Snooze30Min_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromMinutes(30));
        private async void Snooze1Hour_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromHours(1));
        private async void Snooze1Day_Click(object sender, RoutedEventArgs e) => await SnoozeEvent(TimeSpan.FromDays(1));

        private async Task SnoozeEvent(TimeSpan duration)
        {
            var selectedItem = EventsList.SelectedItem as dynamic;
            if (selectedItem?.Event is not CalendarEvent evt || evt.Id == null) 
                return;

            var until = DateTime.Now + duration;
            _configManager.SnoozeEvent(evt.Id, until, evt.StartTime);
            await RefreshEventList();
        }
    }
}