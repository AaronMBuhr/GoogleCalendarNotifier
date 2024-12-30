using System;
using System.Collections.Generic;
using System.Windows.Threading;
using H.NotifyIcon;
using System.Diagnostics;

namespace GoogleCalendarNotifier
{
    public class WindowsNotificationService : INotificationService
    {
        private readonly TaskbarIcon _trayIcon;
        private readonly NotificationSettings _settings;
        private readonly Dictionary<NotificationPopup, DispatcherTimer> _activePopups;
        private readonly Dictionary<string, DateTime> _snoozeTimers;  // EventId -> SnoozeUntil
        private readonly Dictionary<string, DispatcherTimer> _snoozeDispatchTimers;  // EventId -> Timer
        private readonly Dispatcher _dispatcher;
        private readonly EventTrackingService _eventTrackingService;

        public WindowsNotificationService(EventTrackingService eventTrackingService)
        {
            _trayIcon = new TaskbarIcon();
            _settings = new NotificationSettings();
            _activePopups = new Dictionary<NotificationPopup, DispatcherTimer>();
            _snoozeTimers = new Dictionary<string, DateTime>();
            _snoozeDispatchTimers = new Dictionary<string, DispatcherTimer>();
            _dispatcher = Dispatcher.CurrentDispatcher;
            _eventTrackingService = eventTrackingService;
        }

        public void ShowNotification(string title, string message, NotificationType type)
        {
            Debug.WriteLine($"Showing notification - Title: {title}, Type: {type}");
            
            _dispatcher.Invoke(() =>
            {
                var popup = new NotificationPopup();
                
                popup.OnSnooze += (snoozeTime) =>
                {
                    Debug.WriteLine($"Snooze requested for event '{title}' with duration: {snoozeTime}");
                    
                    if (_activePopups.TryGetValue(popup, out var popupTimer))
                    {
                        Debug.WriteLine("Stopping popup timer");
                        popupTimer.Stop();
                        _activePopups.Remove(popup);
                    }

                    if (snoozeTime.HasValue)
                    {
                        var eventId = title; // Using title as ID for now
                        DateTime snoozeUntil;
                        
                        if (snoozeTime.Value == TimeSpan.MaxValue)
                        {
                            Debug.WriteLine("Setting snooze to 'Never'");
                            snoozeUntil = DateTime.MaxValue;
                        }
                        else if (snoozeTime.Value == TimeSpan.Zero)
                        {
                            Debug.WriteLine("Resetting to event time");
                            _snoozeTimers.Remove(eventId);
                            if (_snoozeDispatchTimers.TryGetValue(eventId, out var timer))
                            {
                                timer.Stop();
                                _snoozeDispatchTimers.Remove(eventId);
                            }
                            return;
                        }
                        else
                        {
                            snoozeUntil = DateTime.Now.Add(snoozeTime.Value);
                            Debug.WriteLine($"Setting snooze until: {snoozeUntil}");
                        }

                        _snoozeTimers[eventId] = snoozeUntil;
                        
                        // Clear any existing timer
                        if (_snoozeDispatchTimers.TryGetValue(eventId, out var existingTimer))
                        {
                            Debug.WriteLine("Clearing existing snooze timer");
                            existingTimer.Stop();
                        }

                        // Create new timer for re-notification
                        if (snoozeUntil != DateTime.MaxValue)
                        {
                            var timer = new DispatcherTimer();
                            timer.Interval = snoozeTime.Value;
                            timer.Tick += (s, e) =>
                            {
                                Debug.WriteLine($"Snooze timer elapsed for event: {title}");
                                timer.Stop();
                                _snoozeDispatchTimers.Remove(eventId);
                                _snoozeTimers.Remove(eventId);
                                ShowNotification(title, message, type);
                            };
                            
                            _snoozeDispatchTimers[eventId] = timer;
                            timer.Start();
                            Debug.WriteLine("Started new snooze timer");
                        }

                        // Update event tracking
                        _eventTrackingService.UpdateSnoozeTime(eventId, snoozeUntil);
                    }
                };

                popup.OnDismiss += () =>
                {
                    Debug.WriteLine($"Notification dismissed for event: {title}");
                    if (_activePopups.TryGetValue(popup, out var timer))
                    {
                        timer.Stop();
                        _activePopups.Remove(popup);
                    }
                };

                // Auto-dismiss timer (5 minutes)
                var autoCloseTimer = new DispatcherTimer();
                autoCloseTimer.Interval = TimeSpan.FromMinutes(5);
                autoCloseTimer.Tick += (s, e) =>
                {
                    Debug.WriteLine($"Auto-closing notification for event: {title}");
                    popup.Close();
                    autoCloseTimer.Stop();
                    _activePopups.Remove(popup);
                };

                _activePopups.Add(popup, autoCloseTimer);
                autoCloseTimer.Start();

                // Update tray icon tooltip
                _trayIcon.ToolTipText = type switch
                {
                    NotificationType.Warning => "Warning: " + message,
                    NotificationType.Error => "Error: " + message,
                    NotificationType.Success => "Success: " + message,
                    _ => message
                };

                popup.ShowNotification(title, message);
                Debug.WriteLine($"Notification window shown for event: {title}");
            });
        }

        public void Configure(NotificationSettings settings)
        {
            // Update configurable settings
        }

        ~WindowsNotificationService()
        {
            foreach (var pair in _activePopups)
            {
                pair.Value.Stop();
                pair.Key.Close();
            }
            _activePopups.Clear();

            foreach (var timer in _snoozeDispatchTimers.Values)
            {
                timer.Stop();
            }
            _snoozeDispatchTimers.Clear();
            
            _trayIcon.Dispose();
        }
    }
}