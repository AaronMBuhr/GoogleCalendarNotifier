using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace GoogleCalendarNotifier
{
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private CalendarService? _calendarService;
        private readonly string[] _scopes = { CalendarService.Scope.CalendarReadonly };
        private readonly string _applicationName = "Google Calendar Notifier";
        private readonly string _credentialsPath = "credentials.json";
        private readonly string _tokenPath = "token.json";

        public async Task InitializeAsync()
        {
            try
            {
                using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);

                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(_tokenPath, true));

                // Create the Calendar service
                _calendarService = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName,
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Google Calendar service: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(TimeSpan lookAheadTime)
        {
            if (_calendarService == null)
                throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");

            try
            {
                var now = DateTimeOffset.UtcNow;
                var events = new List<CalendarEvent>();

                var request = _calendarService.Events.List("primary");
                request.TimeMinDateTimeOffset = now;
                request.TimeMaxDateTimeOffset = now.Add(lookAheadTime);
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 100;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                var response = await request.ExecuteAsync();

                foreach (var item in response.Items ?? Enumerable.Empty<Event>())
                {
                    if (item.Start == null || (item.Start.DateTimeDateTimeOffset == null && item.Start.Date == null))
                        continue;

                    // Fix for CS8604 - Ensure we have a valid start date string
                    var start = item.Start.DateTimeDateTimeOffset?.DateTime ??
                               (item.Start.Date != null ? DateTime.Parse(item.Start.Date) : DateTime.UtcNow);

                    // Fix for CS8604 - More careful null handling for end date
                    var end = item.End?.DateTimeDateTimeOffset?.DateTime ??
                             (item.End?.Date != null ? DateTime.Parse(item.End.Date) : start);

                    // Fix for CS8629 - Safe handling of reminder minutes
                    TimeSpan? reminderTime = null;
                    if (item.Reminders?.UseDefault == true)
                    {
                        reminderTime = TimeSpan.FromMinutes(30);
                    }
                    else if (item.Reminders?.Overrides?.FirstOrDefault()?.Minutes is int minutes)
                    {
                        reminderTime = TimeSpan.FromMinutes(minutes);
                    }


                    events.Add(new CalendarEvent
                    {
                        Id = item.Id,
                        Title = item.Summary ?? "",
                        Description = item.Description ?? "",
                        StartTime = start,
                        EndTime = end,
                        IsAllDay = item.Start?.DateTimeDateTimeOffset == null,
                        ReminderTime = reminderTime
                    });
                }

                return events;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to fetch calendar events: {ex.Message}", ex);
            }
        }
    }
}