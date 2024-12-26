#!/bin/bash

# Backup files before making changes
cp MainWindow.xaml MainWindow.xaml.bak
cp MainWindow.xaml.cs MainWindow.xaml.cs.bak

# Step 1: Update MainWindow.xaml
# Replace ni:TaskbarIcon with the correct namespace and element type
sed -i '
s|xmlns:ni="http://schemas.haven-dv.com/notifyicon"|xmlns:ni="http://github.com/HavenDV/H.NotifyIcon"|g;
s/ni:TaskbarIcon/ni:TaskbarIcon/g;
/<Grid Margin="4">/a\
    <Calendar x:Name="MainCalendar" Margin="10" SelectedDatesChanged="MainCalendar_SelectedDatesChanged" />\
    <ListBox x:Name="EventsList" Margin="10,120,10,10" SelectionChanged="EventsList_SelectionChanged" />\
    <TextBlock x:Name="EventDetailsBlock" Margin="10,300,10,10" Text="No event selected." />
' MainWindow.xaml

# Step 2: Update MainWindow.xaml.cs
# Ensure the correct namespace and type is used for TaskbarIcon
sed -i '
s/NotifyIcon/TaskbarIcon/g;
/namespace GoogleCalendarNotifier/a\
using H.NotifyIcon.Wpf;
' MainWindow.xaml.cs

# Step 3: Confirm changes with md5sum
echo "MD5 checksums of modified files:"
md5sum MainWindow.xaml MainWindow.xaml.cs

# Step 4: Confirm word count changes as an additional check
echo "Word count of modified files:"
wc -w MainWindow.xaml MainWindow.xaml.cs

# Notify user of backup files
echo "Backup files created: MainWindow.xaml.bak, MainWindow.xaml.cs.bak"
