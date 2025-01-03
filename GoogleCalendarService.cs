﻿using Google.Apis.Auth.OAuth2;
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
using System.Diagnostics;

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
            Debug.WriteLine("Initializing Google Calendar Service");
            try
            {
                // Delete existing token to force reauthorization
                var tokenDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _tokenPath);
                if (Directory.Exists(tokenDirectory))
                {
                    Debug.WriteLine("Deleting existing token directory");
                    Directory.Delete(tokenDirectory, true);
                }

                using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);
                Debug.WriteLine("Loaded credentials.json");

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(_tokenPath, true));

                Debug.WriteLine("Authorization completed");

                _calendarService = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName,
                });

                Debug.WriteLine("Calendar service created successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex}");
                throw new InvalidOperationException($"Failed to initialize Google Calendar service: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(TimeSpan lookAheadTime)
        {
            Debug.WriteLine($"Getting upcoming events for next {lookAheadTime.TotalDays} days");

            if (_calendarService == null)
                throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");

            try
            {
                var now = DateTime.Now;
                Debug.WriteLine($"Current time: {now}");

                var events = new List<CalendarEvent>();

                var request = _calendarService.Events.List("primary");
                request.TimeMinDateTimeOffset = new DateTimeOffset(now);
                request.TimeMaxDateTimeOffset = new DateTimeOffset(now.Add(lookAheadTime));
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 250;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                Debug.WriteLine($"Requesting events from {request.TimeMin} to {request.TimeMax}");

                var response = await request.ExecuteAsync();
                Debug.WriteLine($"Retrieved {response.Items?.Count ?? 0} events from Google Calendar");

                foreach (var item in response.Items ?? Enumerable.Empty<Event>())
                {
                    Debug.WriteLine($"Processing event: {item.Summary} at {item.Start?.DateTime ?? DateTime.MinValue}");

                    if (item.Start == null || (item.Start.DateTimeDateTimeOffset == null && item.Start.Date == null))
                    {
                        Debug.WriteLine("Skipping event with no start time");
                        continue;
                    }

                    // Parse the start time
                    var start = item.Start.DateTimeDateTimeOffset?.LocalDateTime 
                        ?? (item.Start.Date != null ? DateTime.Parse(item.Start.Date) : DateTime.Now);

                    Debug.WriteLine($"Parsed start time: {start} (Original: {item.Start.DateTimeDateTimeOffset})");

                    // Parse the end time
                    var end = item.End?.DateTimeDateTimeOffset?.LocalDateTime ??
                             (item.End?.Date != null ? DateTime.Parse(item.End.Date) : start);

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
                        ReminderTime = reminderTime
                    });

                    Debug.WriteLine($"Added event to list: {item.Summary}");
                }

                Debug.WriteLine($"Returning {events.Count} events");
                return events;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUpcomingEventsAsync: {ex}");
                throw new InvalidOperationException($"Failed to fetch calendar events: {ex.Message}", ex);
            }
        }

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
    }
}



