﻿using System;
using System.Collections.Generic;
using System.Windows.Threading;
// using H.NotifyIcon; // Removed
using System.Diagnostics;
using System.Windows.Forms; // Added for NotifyIcon, though not directly used in this file after changes

namespace GoogleCalendarNotifier
{
    public class WindowsNotificationService : INotificationService
    {
        // private TaskbarIcon? _trayIcon; // Removed H.NotifyIcon.TaskbarIcon field
        private NotifyIcon? _winFormsTrayIcon; // Kept for potential future use, though SetTrayIcon is commented out
        private readonly NotificationSettings _settings;
        private readonly Dictionary<NotificationPopup, DispatcherTimer> _activePopups;
        private readonly Dictionary<string, DateTime> _snoozeTimers;  // EventId -> SnoozeUntil
        private readonly Dictionary<string, DispatcherTimer> _snoozeDispatchTimers;  // EventId -> Timer
        private readonly Dispatcher _dispatcher;
        private readonly EventTrackingService _eventTrackingService;

        public WindowsNotificationService(EventTrackingService eventTrackingService)
        {
            _settings = new NotificationSettings();
            _activePopups = new Dictionary<NotificationPopup, DispatcherTimer>();
            _snoozeTimers = new Dictionary<string, DateTime>();
            _snoozeDispatchTimers = new Dictionary<string, DispatcherTimer>();
            _dispatcher = Dispatcher.CurrentDispatcher;
            _eventTrackingService = eventTrackingService;
        }

        // public void SetTrayIcon(TaskbarIcon icon) => _trayIcon = icon; // Commented out
        // New signature, also commented out as INotificationService.SetTrayIcon is commented out
        // public void SetTrayIcon(NotifyIcon icon) => _winFormsTrayIcon = icon; 

        public void ShowNotification(string title, string message, NotificationType type, string eventId = null)
        {
            Debug.WriteLine($"Showing notification - Title: {title}, Type: {type}, EventId: {eventId}");
            
            _dispatcher.Invoke(() =>
            {
                var popup = new NotificationPopup();
                
                popup.OnSnooze += (snoozeTime) =>
                {
                    Debug.WriteLine($"Snooze requested for event '{eventId}' with duration: {snoozeTime}");
                    
                    if (_activePopups.TryGetValue(popup, out var popupTimer))
                    {
                        Debug.WriteLine("Stopping popup timer");
                        popupTimer.Stop();
                        _activePopups.Remove(popup);
                    }

                    if (snoozeTime.HasValue && !string.IsNullOrEmpty(eventId))
                    {
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
                                ShowNotification(title, message, type, eventId);
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

                // Update tray icon tooltip if available
                // The _trayIcon field (H.NotifyIcon) was removed. 
                // If tooltip updates are needed for the WinForms NotifyIcon, 
                // this service would need a reference to it, or MainWindow would handle it.
                /* 
                if (_winFormsTrayIcon != null) // Check the new WinForms icon field if it were being set
                {
                    _winFormsTrayIcon.Text = type switch // NotifyIcon uses .Text for tooltip
                    {
                        NotificationType.Warning => "Warning: " + message,
                        NotificationType.Error => "Error: " + message,
                        NotificationType.Success => "Success: " + message,
                        _ => message
                    };
                }
                */

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
            
            // _trayIcon?.Dispose(); // Dispose H.NotifyIcon
            _winFormsTrayIcon?.Dispose(); // Dispose WinForms NotifyIcon if it were used
        }

        public void ShowNotification(string title, string message, NotificationType type)
        {
            ShowNotification(title, message, type, null);
        }
    }
}