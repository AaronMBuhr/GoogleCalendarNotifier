using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
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

        private async System.Threading.Tasks.Task AuthorizeAndCreateServicesAsync()
        {
            using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);
            Debug.WriteLine("Loaded credentials.json for authorization attempt.");

            var tokenDirectory = Path.GetDirectoryName(_tokenPath)!; 
            Debug.WriteLine($"Using token directory: {tokenDirectory} for FileDataStore");

            Debug.WriteLine("Starting OAuth authorization flow...");
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                _scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenDirectory, true)); 

            Debug.WriteLine("Authorization completed successfully.");
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

            Debug.WriteLine("Calendar and Tasks services created successfully.");

            var testRequest = _calendarService.CalendarList.List();
            var testResponse = await testRequest.ExecuteAsync();
            Debug.WriteLine($"Successfully retrieved {testResponse.Items.Count} calendars for testing.");
            
            try
            {
                var taskListRequest = _tasksService.Tasklists.List();
                var taskListResponse = await taskListRequest.ExecuteAsync();
                Debug.WriteLine($"Successfully retrieved {taskListResponse.Items?.Count ?? 0} task lists for testing.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Could not access Tasks API during initial test: {ex.Message}");
            }
        }

        private void DeleteStoredTokenFiles()
        {
            try
            {
                var tokenDirectory = Path.GetDirectoryName(_tokenPath)!;
                // The FileDataStore stores files typically named like "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user"
                string tokenFileUserSpecific = Path.Combine(tokenDirectory, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");
                
                if (File.Exists(tokenFileUserSpecific))
                {
                    File.Delete(tokenFileUserSpecific);
                    Debug.WriteLine($"Deleted stale token file: {tokenFileUserSpecific}");
                }
                else
                {
                    Debug.WriteLine($"Stale token file not found at: {tokenFileUserSpecific}, checking for legacy token.json.");
                    if (File.Exists(_tokenPath)) // Check for the legacy token.json
                    {
                        File.Delete(_tokenPath);
                        Debug.WriteLine($"Deleted legacy token file: {_tokenPath}");
                    }
                    else
                    {
                        Debug.WriteLine($"Legacy token file not found at: {_tokenPath}");
                    }
                }
            }
            catch (Exception deleteEx)
            {
                Debug.WriteLine($"Error deleting token file(s): {deleteEx.Message}");
                // Non-critical, can proceed.
            }
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            Debug.WriteLine("Initializing Google Calendar Service");
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                    Debug.WriteLine($"Created directory: {AppDataPath}");
                }
                
                if (!File.Exists(_credentialsPath))
                {
                    var errorMessage = $"credentials.json not found at {_credentialsPath}";
                    Debug.WriteLine(errorMessage);
                    var detailedInstructions = $""""{errorMessage}\n\nTo use this application, you need OAuth 2.0 credentials:\n\n1. Go to Google Cloud Console (console.cloud.google.com).\n2. Select/create a project.\n3. Enable 'Google Calendar API' & 'Google Tasks API' (APIs & Services > Library).\n4. Configure 'OAuth consent screen' (add required scopes & test users).\n5. Create 'OAuth client ID' (Credentials > Create > OAuth client ID > Desktop app).\n6. Download the client secret JSON file.\n7. Rename the downloaded file to 'credentials.json'.\n8. Place 'credentials.json' in this directory:\n{AppDataPath}\n"""";
                    System.Windows.MessageBox.Show(detailedInstructions, "Missing Credentials File", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new FileNotFoundException(errorMessage, _credentialsPath);
                }
                Debug.WriteLine($"Found credentials.json at {_credentialsPath}");
                
                await AuthorizeAndCreateServicesAsync(); // First attempt
            }
            catch (TokenResponseException tex) when (tex.Error != null && tex.Error.Error == "invalid_grant")
            {
                Debug.WriteLine($"Initial authorization failed with TokenResponseException (invalid_grant): {tex.Message}");
                DeleteStoredTokenFiles();

                System.Windows.MessageBox.Show(
                    "Your Google authentication has expired or needs to be refreshed. The application will now attempt to guide you through the re-authentication process with Google. Please follow the prompts in your web browser.",
                    "Re-authentication Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                try
                {
                    Debug.WriteLine("Attempting re-authorization...");
                    await AuthorizeAndCreateServicesAsync(); // Second attempt
                    Debug.WriteLine("Re-authorization successful.");
                }
                catch (Exception exReAuth)
                {
                    Debug.WriteLine($"Re-authorization attempt failed: {exReAuth}");
                    System.Windows.MessageBox.Show(
                        $"Failed to re-authenticate with Google: {exReAuth.Message}\n\nPlease restart the application to try again or check your Google account settings.",
                        "Re-authentication Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw; 
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex}");
                System.Windows.MessageBox.Show(
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

            if (_calendarService == null || _tasksService == null)
                throw new InvalidOperationException("Calendar or Tasks service not initialized. Call InitializeAsync first.");

            try
            {
                var now = DateTime.Now;
                var endDate = now.Add(lookAheadTime);
                Debug.WriteLine($"Current time: {now}, fetching upcoming until {endDate}");

                var events = new List<CalendarEvent>();
                
                await GetCalendarEventsInRange(now, endDate, includeHolidays, events);
                await GetGoogleTasksInRange(now, endDate, events); // Use new ranged method

                Debug.WriteLine($"Returning {events.Count} events (including tasks) for upcoming period");
                return events;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUpcomingEventsAsync: {ex}");
                throw new InvalidOperationException($"Failed to fetch calendar events: {ex.Message}", ex);
            }
        }
        
        private async System.Threading.Tasks.Task GetCalendarEventsInRange(DateTime queryStartDate, DateTime queryEndDate, bool includeHolidays, List<CalendarEvent> events)
        {
            // First get the calendars to query
            var calendarListRequest = _calendarService.CalendarList.List();
            var calendarListResponse = await calendarListRequest.ExecuteAsync();
            var calendars = calendarListResponse.Items;
            
            Debug.WriteLine($"Found {calendars.Count} calendars to query for range {queryStartDate:d} to {queryEndDate:d}");
            
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
                request.TimeMinDateTimeOffset = new DateTimeOffset(queryStartDate);
                request.TimeMaxDateTimeOffset = new DateTimeOffset(queryEndDate);
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 2500; // Increased max results for potentially larger date ranges
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Debug.WriteLine($"Requesting events from {request.TimeMinDateTimeOffset} to {request.TimeMaxDateTimeOffset}");

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
        
        // Refactored to take specific start and end dates for tasks
        private async System.Threading.Tasks.Task GetGoogleTasksInRange(DateTime queryStartDate, DateTime queryEndDate, List<CalendarEvent> events)
        {
            if (_tasksService == null) 
            {
                Debug.WriteLine("Tasks service not initialized, skipping task retrieval.");
                return;
            }

            try
            {
                Debug.WriteLine($"Retrieving Google Tasks from {queryStartDate:d} to {queryEndDate:d}...");
                
                var taskListRequest = _tasksService.Tasklists.List();
                var taskLists = await taskListRequest.ExecuteAsync();
                
                if (taskLists.Items == null || taskLists.Items.Count == 0)
                {
                    Debug.WriteLine("No task lists found");
                    return;
                }
                
                foreach (var taskList in taskLists.Items)
                {
                    var tasksRequest = _tasksService.Tasks.List(taskList.Id);
                    tasksRequest.ShowCompleted = true;
                    tasksRequest.MaxResults = 100;
                    // RFC3339 format for DueMin and DueMax
                    tasksRequest.DueMin = queryStartDate.ToUniversalTime().ToString("o"); 
                    tasksRequest.DueMax = queryEndDate.ToUniversalTime().ToString("o");
                    
                    Debug.WriteLine($"Requesting tasks from list {taskList.Title} with DueMin: {tasksRequest.DueMin} and DueMax: {tasksRequest.DueMax}");
                    var tasks = await tasksRequest.ExecuteAsync();
                    
                    if (tasks.Items == null) continue;
                    
                    foreach (var task in tasks.Items)
                    {
                        if (string.IsNullOrEmpty(task.Due)) continue;
                        
                        try
                        {
                            DateTime dueDate;
                            bool isDueMidnightUTC = false;
                            if (DateTime.TryParse(task.Due, out dueDate))
                            {
                                if (task.Due.Contains("T00:00:00") && (task.Due.EndsWith("Z") || task.Due.Contains("+00:00")))
                                    isDueMidnightUTC = true;
                                if (task.Due.EndsWith("Z") || task.Due.Contains("+00:00"))
                                {
                                    dueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc).ToLocalTime();
                                }
                            }
                            else continue;

                            // Additional client-side filtering as DueMin/DueMax might not be perfectly precise
                            // or if tasks span across the midnight boundary in a way the API filter misses for local time.
                            if (dueDate.Date < queryStartDate.Date || dueDate.Date > queryEndDate.Date) 
                            {
                                Debug.WriteLine($"Skipping task {task.Title} due {dueDate:d} (client-side filter) as it's outside range {queryStartDate:d}-{queryEndDate:d}");
                                continue;
                            }
                            
                            if (isDueMidnightUTC)
                            {
                                string originalDatePart = task.Due.Split('T')[0];
                                string[] dateParts = originalDatePart.Split('-');
                                if (dateParts.Length == 3 && int.TryParse(dateParts[0], out int y) && int.TryParse(dateParts[1], out int m) && int.TryParse(dateParts[2], out int d))
                                {
                                    dueDate = new DateTime(y, m, d, 9, 0, 0);
                                }
                            }
                            
                            var startTime = dueDate; 
                            var taskTitle = task.Title ?? "Untitled Task";
                            events.Add(new CalendarEvent
                            {
                                Id = task.Id ?? Guid.NewGuid().ToString(),
                                Title = taskTitle,
                                Description = task.Notes ?? "",
                                StartTime = startTime,
                                EndTime = startTime.AddHours(1),
                                IsAllDay = startTime.TimeOfDay == TimeSpan.Zero && isDueMidnightUTC, 
                                ReminderTime = TimeSpan.FromMinutes(30),
                                CalendarId = taskList.Id,
                                CalendarName = $"{taskList.Title} (Tasks)",
                                IsHoliday = false,
                                IsTask = true,
                                IsCompleted = task.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false
                            });
                            Debug.WriteLine($"Added task to event list: {task.Title} due {startTime:g}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing task {task.Title}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving Google Tasks for range: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime startDate, DateTime endDate, bool includeHolidays = true)
        {
            Debug.WriteLine($"Getting events and tasks between {startDate:d} and {endDate:d}");

            if (_calendarService == null)
                throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");

            try
            {
                var events = new List<CalendarEvent>();
                await GetCalendarEventsInRange(startDate, endDate, includeHolidays, events);
                await GetGoogleTasksInRange(startDate, endDate, events); // Add call to fetch tasks for the range

                Debug.WriteLine($"Returning {events.Count} calendar events and tasks for range {startDate:d} to {endDate:d}");
                return events;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetEventsAsync (range): {ex}");
                throw new InvalidOperationException($"Failed to fetch calendar events and tasks for specific range: {ex.Message}", ex);
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



