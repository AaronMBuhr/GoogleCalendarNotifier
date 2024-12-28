#!/bin/bash

# Define target files
SERVICE_FILE="GoogleCalendarService.cs"
MAIN_WINDOW_CS="MainWindow.xaml.cs"
MAIN_WINDOW_XAML="MainWindow.xaml"

# Backup original files
cp "$SERVICE_FILE" "$SERVICE_FILE.bak"
cp "$MAIN_WINDOW_CS" "$MAIN_WINDOW_CS.bak"
cp "$MAIN_WINDOW_XAML" "$MAIN_WINDOW_XAML.bak"

# Update GoogleCalendarService.cs
sed -i \
    '/var start = item.Start.DateTimeDateTimeOffset?.ToLocalTime()/c\
            var start = item.Start.DateTimeDateTimeOffset?.UtcDateTime ?? (item.Start.Date != null ? DateTime.Parse(item.Start.Date) : DateTime.Now);' \
    "$SERVICE_FILE"

sed -i \
    '/var end = item.End?.DateTimeDateTimeOffset?.ToLocalTime()/c\
            var end = item.End?.DateTimeDateTimeOffset?.UtcDateTime ?? (item.End?.Date != null ? DateTime.Parse(item.End.Date) : start);' \
    "$SERVICE_FILE"

# Update MainWindow.xaml.cs
sed -i \
    '/private async void MainCalendar_SelectedDatesChanged/c\
    private async void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e) {\
        if (MainCalendar.SelectedDate.HasValue) {\
            await RefreshEventList();\
        }\
    }' \
    "$MAIN_WINDOW_CS"

sed -i \
    '/private async Task RefreshEventList()/c\
    private async Task RefreshEventList() {\
        if (!MainCalendar.SelectedDate.HasValue) return;\
\
        var selectedDate = MainCalendar.SelectedDate.Value;\
        var todaysEvents = _monthEvents.Where(e => e.StartTime.Date == selectedDate.Date)\
                                       .OrderBy(e => e.StartTime);\
\
        var items = todaysEvents.Select(evt => {\
            var snoozeInfo = _configManager.GetEventSnoozeInfo(evt.Id);\
            string notificationTime = snoozeInfo != null ? \
                $\"Snoozed until {snoozeInfo.UntilTime:HH:mm}\" : \
                \"Not snoozed\";\
\
            return new EventListItem {\
                DateTime = evt.StartTime.ToString(\"g\"),\
                Title = evt.Title,\
                NotificationTime = notificationTime,\
                IsSelectedDay = true,\
                Event = evt\
            };\
        }).ToList();\
\
        EventsList.ItemsSource = items;\
\
        if (items.Any()) {\
            EventDetailsBlock.Text = \"Event details loaded.\";\
        } else {\
            EventDetailsBlock.Text = \"No events for the selected date.\";\
        }\
    }' \
    "$MAIN_WINDOW_CS"

sed -i \
    '/private void EventsList_SelectionChanged/c\
    private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {\
        var item = EventsList.SelectedItem as EventListItem;\
        if (item?.Event != null) {\
            MainCalendar.SelectedDate = item.Event.StartTime.Date;\
        }\
    }' \
    "$MAIN_WINDOW_CS"

# Update MainWindow.xaml
sed -i \
    '/<Calendar x:Name="MainCalendar"/c\
        <Calendar x:Name="MainCalendar" \
                 SelectionMode="SingleDate" \
                 SelectedDatesChanged="MainCalendar_SelectedDatesChanged" \
                 Loaded="MainCalendar_Loaded" \
                 Margin="1">' \
    "$MAIN_WINDOW_XAML"

sed -i \
    '/<ListView x:Name="EventsList"/c\
        <ListView x:Name="EventsList" \
                 SelectionChanged="EventsList_SelectionChanged">' \
    "$MAIN_WINDOW_XAML"

# Print success message
echo "Modifications applied successfully. Backups created with .bak extension."
