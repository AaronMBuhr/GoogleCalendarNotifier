using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows;

namespace GoogleCalendarNotifier
{
    using TaskItem = Google.Apis.Tasks.v1.Data.Task;
    
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private CalendarService? _calendarService;
        private TasksService? _tasksService;
        private readonly string[] _scopes = { 
            CalendarService.Scope.CalendarReadonly, 
            "https://www.googleapis.com/auth/tasks",
            "https://www.googleapis.com/auth/tasks.readonly"
        };
        private readonly string _applicationName = "Google Calendar Notifier";
        
        // Use LocalAppData for storing credentials and token
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoogleCalendarNotifier");
        private readonly string _credentialsPath = Path.Combine(AppDataPath, "credentials.json");
        private readonly string _tokenPath = Path.Combine(AppDataPath, "token.json"); // Store token here as well

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            Debug.WriteLine("Initializing Google Calendar Service");
            try
            {
                // Ensure the AppData directory exists
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                    Debug.WriteLine($"Created directory: {AppDataPath}");
                }
                
                // Check if credentials.json exists in the AppData path
                if (!File.Exists(_credentialsPath))
                {
                    var errorMessage = $"credentials.json not found at {_credentialsPath}";
                    Debug.WriteLine(errorMessage);

                    // Provide more detailed instructions
                    var detailedInstructions = $"""
{errorMessage}

To use this application, you need OAuth 2.0 credentials:

1. Go to Google Cloud Console (console.cloud.google.com).
2. Select/create a project.
3. Enable 'Google Calendar API' & 'Google Tasks API' (APIs & Services > Library).
4. Configure 'OAuth consent screen' (add required scopes & test users).
5. Create 'OAuth client ID' (Credentials > Create > OAuth client ID > Desktop app).
6. Download the client secret JSON file.
7. Rename the downloaded file to 'credentials.json'.
8. Place 'credentials.json' in this directory:
{AppDataPath}
""";

                    MessageBox.Show(
                        detailedInstructions,
                        "Missing Credentials File",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw new FileNotFoundException(errorMessage, _credentialsPath);
                }

                Debug.WriteLine($"Found credentials.json at {_credentialsPath}");
                
                // Get the token directory path (now using the AppData path)
                var tokenDirectory = Path.GetDirectoryName(_tokenPath); // Use directory for FileDataStore
                Debug.WriteLine($"Token directory: {tokenDirectory}");
                
                // Only force token refresh if we need to upgrade scopes - uncomment this block when changing required scopes
                /*
                if (Directory.Exists(tokenDirectory))
                {
                    Debug.WriteLine("Deleting existing token directory to refresh scopes");
                    Directory.Delete(tokenDirectory, true);
                }
                */

                using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);
                Debug.WriteLine("Loaded credentials.json");

                Debug.WriteLine("Starting OAuth authorization flow...");
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenDirectory, true)); // Pass the directory path

                Debug.WriteLine("Authorization completed successfully");
                Debug.WriteLine($"Token acquired for account: {credential.UserId}");

                _calendarService = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName,
                });

                _tasksService = new TasksService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName,
                });

                Debug.WriteLine("Calendar and Tasks services created successfully");
                
                // Test if we can access the API
                var testRequest = _calendarService.CalendarList.List();
                var testResponse = await testRequest.ExecuteAsync();
                Debug.WriteLine($"Successfully retrieved {testResponse.Items.Count} calendars");
                foreach (var calendar in testResponse.Items)
                {
                    Debug.WriteLine($"Calendar: {calendar.Summary} (ID: {calendar.Id})");
                }
                
                // Test task access
                try
                {
                    var taskListRequest = _tasksService.Tasklists.List();
                    var taskListResponse = await taskListRequest.ExecuteAsync();
                    Debug.WriteLine($"Successfully retrieved {taskListResponse.Items?.Count ?? 0} task lists");
                    
                    foreach (var taskList in taskListResponse.Items ?? Enumerable.Empty<TaskList>())
                    {
                        Debug.WriteLine($"Task List: {taskList.Title} (ID: {taskList.Id})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Warning: Could not access Tasks API: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex}");
                MessageBox.Show(
                    $"Failed to initialize Google Calendar service: {ex.Message}\n\n{ex.StackTrace}",
                    "Calendar Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw;
            }
        }

        public async System.Threading.Tasks.Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(TimeSpan lookAheadTime, bool includeHolidays = true)
        {
            Debug.WriteLine($"Getting upcoming events for next {lookAheadTime.TotalDays} days");

            if (_calendarService == null)
                throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");

            try
            {
                var now = DateTime.Now;
                Debug.WriteLine($"Current time: {now}");

                var events = new List<CalendarEvent>();
                
                // Get calendar events
                await GetCalendarEvents(lookAheadTime, includeHolidays, now, events);
                
                // Get tasks if the service is available
                if (_tasksService != null)
                {
                    await GetGoogleTasks(lookAheadTime, now, events);
                }

                Debug.WriteLine($"Returning {events.Count} events (including tasks)");
                return events;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUpcomingEventsAsync: {ex}");
                throw new InvalidOperationException($"Failed to fetch calendar events: {ex.Message}", ex);
            }
        }
        
        private async System.Threading.Tasks.Task GetCalendarEvents(TimeSpan lookAheadTime, bool includeHolidays, DateTime now, List<CalendarEvent> events)
        {
            // First get the calendars to query
            var calendarListRequest = _calendarService.CalendarList.List();
            var calendarListResponse = await calendarListRequest.ExecuteAsync();
            var calendars = calendarListResponse.Items;
            
            Debug.WriteLine($"Found {calendars.Count} calendars to query");
            
            // Filter out holiday calendars if includeHolidays is false
            List<CalendarListEntry> calendarsToQuery;
            if (includeHolidays)
            {
                calendarsToQuery = calendars.ToList();
            }
            else
            {
                // Exclude holiday and global calendars
                calendarsToQuery = calendars
                    .Where(c => !IsHolidayCalendar(c))
                    .ToList();
                
                Debug.WriteLine($"Filtered to {calendarsToQuery.Count} non-holiday calendars");
            }
            
            foreach (var calendar in calendarsToQuery)
            {
                Debug.WriteLine($"Querying calendar: {calendar.Summary} (ID: {calendar.Id})");
                
                var request = _calendarService.Events.List(calendar.Id);
                request.TimeMinDateTimeOffset = new DateTimeOffset(now);
                request.TimeMaxDateTimeOffset = new DateTimeOffset(now.Add(lookAheadTime));
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 250;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Debug.WriteLine($"Requesting events from {request.TimeMin} to {request.TimeMax}");

                var response = await request.ExecuteAsync();
                Debug.WriteLine($"Retrieved {response.Items?.Count ?? 0} events from calendar {calendar.Summary}");

                if (response.Items == null) continue;
                
                foreach (var item in response.Items)
                {
                    Debug.WriteLine($"Processing event: {item.Summary} at {item.Start?.DateTime ?? DateTime.MinValue}, All day: {item.Start?.DateTime == null}");

                    if (item.Start == null || (item.Start.DateTimeDateTimeOffset == null && item.Start.Date == null))
                    {
                        Debug.WriteLine("Skipping event with no start time");
                        continue;
                    }

                    // Parse the start time
                    var start = item.Start.DateTimeDateTimeOffset?.LocalDateTime 
                        ?? (item.Start.Date != null ? DateTime.Parse(item.Start.Date) : DateTime.Now);

                    // Ensure we're using the correct time zone
                    if (item.Start.TimeZone != null && item.Start.DateTime != null)
                    {
                        // Try to use the event's specific time zone if provided
                        try
                        {
                            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(item.Start.TimeZone);
                            var dateTimeString = item.Start.DateTime.ToString();
                            var utcDateTime = DateTime.Parse(dateTimeString).ToUniversalTime();
                            start = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
                            Debug.WriteLine($"Applied specific time zone {item.Start.TimeZone} to event: {start}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error applying time zone {item.Start.TimeZone}: {ex.Message}");
                        }
                    }

                    Debug.WriteLine($"Parsed start time: {start} (Original: {item.Start.DateTimeDateTimeOffset})");

                    // Parse the end time
                    var end = item.End?.DateTimeDateTimeOffset?.LocalDateTime ??
                            (item.End?.Date != null ? DateTime.Parse(item.End.Date) : start);
                    
                    // Apply the same time zone to end time if needed
                    if (item.End?.TimeZone != null && item.End?.DateTime != null)
                    {
                        try
                        {
                            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(item.End.TimeZone);
                            var dateTimeString = item.End.DateTime.ToString();
                            var utcDateTime = DateTime.Parse(dateTimeString).ToUniversalTime();
                            end = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
                        }
                        catch
                        {
                            // Already logged for start time
                        }
                    }

                    Debug.WriteLine($"Parsed end time: {end}");

                    // Handle reminders
                    TimeSpan? reminderTime = null;
                    if (item.Reminders?.UseDefault == true)
                    {
                        reminderTime = TimeSpan.FromMinutes(30);
                        Debug.WriteLine("Using default reminder time (30 minutes)");
                    }
                    else if (item.Reminders?.Overrides?.FirstOrDefault()?.Minutes is int minutes)
                    {
                        reminderTime = TimeSpan.FromMinutes(minutes);
                        Debug.WriteLine($"Using custom reminder time ({minutes} minutes)");
                    }

                    events.Add(new CalendarEvent
                    {
                        Id = item.Id,
                        Title = item.Summary ?? "",
                        Description = item.Description ?? "",
                        StartTime = start,
                        EndTime = end,
                        IsAllDay = item.Start.DateTime == null,
                        ReminderTime = reminderTime,
                        CalendarId = calendar.Id,
                        CalendarName = calendar.Summary,
                        IsHoliday = IsHolidayCalendar(calendar),
                        IsTask = false
                    });

                    Debug.WriteLine($"Added event to list: {item.Summary}");
                }
            }
        }
        
        private async System.Threading.Tasks.Task GetGoogleTasks(TimeSpan lookAheadTime, DateTime now, List<CalendarEvent> events)
        {
            try
            {
                Debug.WriteLine("Retrieving Google Tasks...");
                
                // Get all task lists
                var taskListRequest = _tasksService.Tasklists.List();
                var taskLists = await taskListRequest.ExecuteAsync();
                
                if (taskLists.Items == null || taskLists.Items.Count == 0)
                {
                    Debug.WriteLine("No task lists found");
                    return;
                }
                
                Debug.WriteLine($"Found {taskLists.Items.Count} task lists");
                
                // Log task list details
                foreach (var taskList in taskLists.Items)
                {
                    Debug.WriteLine($"Task list: ID={taskList.Id}, Title={taskList.Title}");
                }
                
                var maxDate = now.Add(lookAheadTime);
                
                foreach (var taskList in taskLists.Items)
                {
                    Debug.WriteLine($"Processing task list: {taskList.Title}");
                    
                    // Get tasks for this list
                    var tasksRequest = _tasksService.Tasks.List(taskList.Id);
                    tasksRequest.ShowCompleted = true; // Show both completed and incomplete tasks
                    tasksRequest.MaxResults = 100;
                    
                    Debug.WriteLine($"Requesting tasks from list {taskList.Title}");
                    var tasks = await tasksRequest.ExecuteAsync();
                    
                    if (tasks.Items == null)
                    {
                        Debug.WriteLine($"No tasks found in list {taskList.Title}");
                        continue;
                    }
                    
                    Debug.WriteLine($"Found {tasks.Items.Count} tasks in list {taskList.Title}");
                    
                    // Log all tasks in this list
                    foreach (var task in tasks.Items)
                    {
                        Debug.WriteLine($"Task: ID={task.Id}, Title={task.Title}, Due={(task.Due ?? "null")}, Completed={(task.Completed ?? "null")}, Status={task.Status}");
                    }
                    
                    foreach (var task in tasks.Items)
                    {
                        if (string.IsNullOrEmpty(task.Due))
                        {
                            Debug.WriteLine($"Skipping task without due date: {task.Title}");
                            continue;
                        }
                        
                        try
                        {
                            Debug.WriteLine($"Processing task with due date: {task.Title}, Due={task.Due}");
                            
                            // Different formats Google might return for dates
                            DateTime dueDate;
                            bool isDueMidnightUTC = false;
                            
                            if (DateTime.TryParse(task.Due, out dueDate))
                            {
                                Debug.WriteLine($"Successfully parsed due date: {dueDate}");
                                Debug.WriteLine($"Original due string from Google: {task.Due}");
                                
                                // Check if the original UTC time was midnight before conversion
                                if (task.Due.Contains("T00:00:00") && (task.Due.EndsWith("Z") || task.Due.Contains("+00:00")))
                                {
                                    isDueMidnightUTC = true;
                                    Debug.WriteLine("Task is due at midnight UTC - treating as date-only task");
                                }
                                
                                // Google Tasks API returns dates in UTC - convert to local time
                                if (task.Due.EndsWith("Z") || task.Due.Contains("+00:00"))
                                {
                                    // The date is in UTC, but DateTime.Parse doesn't set Kind correctly
                                    // We need to explicitly specify the Kind before conversion
                                    dueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
                                    dueDate = dueDate.ToLocalTime();
                                    Debug.WriteLine($"Converted UTC date to local: {dueDate}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"Failed to parse due date string: {task.Due}");
                                continue;
                            }
                            
                            // Skip tasks that are due beyond our look-ahead time
                            if (dueDate > maxDate)
                            {
                                Debug.WriteLine($"Skipping task due beyond look-ahead time: {task.Title} due {dueDate}");
                                continue;
                            }
                            
                            // Skip tasks that are too old (more than 7 days in the past)
                            if (dueDate < now.AddDays(-7))
                            {
                                Debug.WriteLine($"Skipping old task: {task.Title} due {dueDate}");
                                continue;
                            }
                            
                            // For tasks that Google reports with midnight UTC, they are likely date-only tasks
                            // We need to preserve the original intended date
                            if (isDueMidnightUTC)
                            {
                                // Extract the original date from the Google Task API string
                                // Format is typically: "2025-05-09T00:00:00.000Z"
                                string originalDatePart = task.Due.Split('T')[0]; // Gets "2025-05-09"
                                string[] dateParts = originalDatePart.Split('-');
                                
                                if (dateParts.Length == 3 && 
                                    int.TryParse(dateParts[0], out int year) && 
                                    int.TryParse(dateParts[1], out int month) && 
                                    int.TryParse(dateParts[2], out int day))
                                {
                                    // Preserve the exact date from the original string
                                    // Use 9:00 AM as the default time for tasks specified with only a date
                                    dueDate = new DateTime(year, month, day, 9, 0, 0);
                                    Debug.WriteLine($"Adjusted task to use original date at 9:00 AM: {dueDate}");
                                }
                            }
                            
                            // Extract time information if available, otherwise assume beginning of day
                            var dueTime = dueDate.TimeOfDay;
                            var startTime = dueDate.Date.Add(dueTime);
                            
                            Debug.WriteLine($"Adding task: {task.Title} due {startTime}");
                            
                            var taskTitle = task.Title ?? "Untitled Task";
                            
                            // Special case logging for "Order Meds" task
                            if (taskTitle.Contains("Order") && taskTitle.Contains("Med"))
                            {
                                Debug.WriteLine($"*** FOUND ORDER MEDS TASK: {taskTitle} due {startTime} ***");
                            }
                            
                            events.Add(new CalendarEvent
                            {
                                Id = task.Id ?? Guid.NewGuid().ToString(),
                                Title = taskTitle,
                                Description = task.Notes ?? "",
                                StartTime = startTime,
                                EndTime = startTime.AddHours(1), // Assume 1 hour duration for tasks
                                IsAllDay = dueTime.TotalMinutes == 0, // If no time specified, treat as all-day
                                ReminderTime = TimeSpan.FromMinutes(30), // Default reminder time for tasks
                                CalendarId = taskList.Id,
                                CalendarName = $"{taskList.Title} (Tasks)",
                                IsHoliday = false,
                                IsTask = true,
                                IsCompleted = task.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false
                            });
                            
                            Debug.WriteLine($"Task added to event list: {task.Title}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing task {task.Title}: {ex.Message}");
                            Debug.WriteLine($"Exception details: {ex}");
                        }
                    }
                }
                
                // Also try to get tasks from calendar-based tasks
                try
                {
                    Debug.WriteLine("Checking for calendar-based tasks...");
                    
                    // Some tasks appear as regular events with special markers
                    var calendarEvents = events.Where(e => !e.IsTask && !e.IsHoliday).ToList();
                    foreach (var evt in calendarEvents)
                    {
                        // Check if the event title or description contains task-related keywords
                        var title = evt.Title.ToLowerInvariant();
                        var desc = evt.Description.ToLowerInvariant();
                        
                        if (title.Contains("task") || title.Contains("todo") || 
                            title.Contains("to do") || title.StartsWith("☐") ||
                            title.StartsWith("□") || title.StartsWith("✓") ||
                            title.Contains("order") || title.Contains("buy") ||
                            title.Contains("pick up") || title.Contains("reminder"))
                        {
                            Debug.WriteLine($"Found potential task in calendar event: {evt.Title}");
                            evt.IsTask = true;
                        }
                        
                        // Special case for "Order Meds" event/task
                        if (evt.Title.Contains("Order") && evt.Title.Contains("Med"))
                        {
                            Debug.WriteLine($"*** FOUND ORDER MEDS AS CALENDAR EVENT: {evt.Title} at {evt.StartTime} ***");
                            evt.IsTask = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking for calendar-based tasks: {ex.Message}");
                }
                
                Debug.WriteLine($"Added {events.Count(e => e.IsTask)} tasks to events list");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving tasks: {ex.Message}");
                Debug.WriteLine($"Task retrieval error details: {ex}");
                MessageBox.Show(
                    $"Error retrieving tasks: {ex.Message}\n\nPlease check you have enabled the Tasks API in your Google Cloud project.",
                    "Tasks API Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public System.Threading.Tasks.Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate, bool includeHolidays = true)
        {
            Debug.WriteLine($"Getting events between {startDate} and {endDate}");

            if (_calendarService == null)
                throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");

            try
            {
                return GetUpcomingEventsAsync(endDate - startDate, includeHolidays);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetEventsAsync: {ex}");
                throw new InvalidOperationException($"Failed to fetch calendar events: {ex.Message}", ex);
            }
        }
        
        private bool IsHolidayCalendar(CalendarListEntry calendar)
        {
            if (calendar == null) return false;
             
            // Check if this is a holiday calendar or other global calendar
            var name = calendar.Summary?.ToLowerInvariant() ?? "";
            var description = calendar.Description?.ToLowerInvariant() ?? "";
             
            // Common holiday calendar identifiers
            var holidayKeywords = new[] { 
                "holiday", "holidays", "vacation", 
                "us holidays", "christian holidays", "jewish holidays",
                "statutory holidays", "festive", "national holiday",
                "birthdays", "birth days", "important dates"
            };
             
            // Check for holiday keywords in name or description
            return holidayKeywords.Any(keyword => 
                name.Contains(keyword) || description.Contains(keyword));
        }
        
        public async System.Threading.Tasks.Task<bool> CompleteTaskAsync(string taskId, string taskListId)
        {
            if (_tasksService == null)
                throw new InvalidOperationException("Tasks service not initialized. Call InitializeAsync first.");
                
            try
            {
                Debug.WriteLine($"Marking task as completed: TaskId={taskId}, TaskListId={taskListId}");
                
                // First, get the current task to update it
                var getRequest = _tasksService.Tasks.Get(taskListId, taskId);
                var task = await getRequest.ExecuteAsync();
                
                if (task == null)
                {
                    Debug.WriteLine($"Task not found: TaskId={taskId}");
                    return false;
                }
                
                // Set completion status
                task.Status = "completed";
                task.Completed = DateTime.UtcNow.ToString("o"); // ISO 8601 format with 'o' format specifier
                
                // Update the task in Google Tasks
                var updateRequest = _tasksService.Tasks.Update(task, taskListId, taskId);
                var updatedTask = await updateRequest.ExecuteAsync();
                
                Debug.WriteLine($"Task marked as completed: {updatedTask.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error completing task: {ex.Message}");
                return false;
            }
        }
    }
}



