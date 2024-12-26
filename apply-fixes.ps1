# PowerShell script to apply fixes
Write-Host "Applying fixes..." -ForegroundColor Green

# 1. Fix NotifyIcon in MainWindow.xaml
$mainWindowXaml = Get-Content "MainWindow.xaml" -Raw
$mainWindowXaml = $mainWindowXaml -replace '<ni:TaskbarIcon x:Name="NotifyIcon"', '<ni:TaskbarIcon x:Name="NotifyIcon"'
$mainWindowXaml = $mainWindowXaml -replace 'TrayMouseDoubleClick="TrayIcon_TrayMouseDoubleClick"', 'TrayMouseDoubleClick="NotifyIcon_TrayMouseDoubleClick"'
$mainWindowXaml = $mainWindowXaml -replace '<ni:TaskbarIcon x:Name="TrayIcon"', '<ni:TaskbarIcon x:Name="NotifyIcon"'
Set-Content "MainWindow.xaml" $mainWindowXaml -NoNewline

# 2. Update GoogleCalendarService.cs to add GetEventsAsync method
$serviceContent = Get-Content "GoogleCalendarService.cs" -Raw
$insertPoint = $serviceContent.LastIndexOf("}")
$newMethod = @"

        public Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate)
        {
            Debug.WriteLine($"Getting events between {startDate} and {endDate}");
            
            if (_calendarService == null)
                throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");

            try
            {
                return GetUpcomingEventsAsync(endDate - startDate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetEventsAsync: {ex}");
                throw new InvalidOperationException($"Failed to fetch calendar events: {ex.Message}", ex);
            }
        }
"@

$updatedService = $serviceContent.Insert($insertPoint, $newMethod)
Set-Content "GoogleCalendarService.cs" $updatedService -NoNewline

# 3. Update CalendarEvent.cs to ensure nullable reference types
$calendarEvent = @"
using System;

namespace GoogleCalendarNotifier
{
    public class CalendarEvent
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsAllDay { get; set; }
        public TimeSpan? ReminderTime { get; set; }
    }
}
"@
Set-Content "CalendarEvent.cs" $calendarEvent -NoNewline

# 4. Update MainWindow.xaml.cs to fix event handler name
$mainWindowCs = Get-Content "MainWindow.xaml.cs" -Raw
$mainWindowCs = $mainWindowCs -replace "TrayIcon_TrayMouseDoubleClick", "NotifyIcon_TrayMouseDoubleClick"
Set-Content "MainWindow.xaml.cs" $mainWindowCs -NoNewline

Write-Host "All fixes applied successfully!" -ForegroundColor Green
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")