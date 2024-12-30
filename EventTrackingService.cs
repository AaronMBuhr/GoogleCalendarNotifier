using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace GoogleCalendarNotifier
{
    public class EventTrackingService
    {
        private readonly Dictionary<string, DateTime> _snoozeTimers = new Dictionary<string, DateTime>();
        private event EventHandler<string> SnoozeTimeUpdated;
        
        public void UpdateSnoozeTime(string eventId, DateTime snoozeUntil)
        {
            Debug.WriteLine($"Updating snooze time for event {eventId} to {snoozeUntil}");
            
            if (snoozeUntil <= DateTime.Now && snoozeUntil != DateTime.MaxValue)
            {
                Debug.WriteLine("Clearing snooze time as it's in the past");
                _snoozeTimers.Remove(eventId);
            }
            else
            {
                _snoozeTimers[eventId] = snoozeUntil;
            }
            
            SnoozeTimeUpdated?.Invoke(this, eventId);
        }

        public void ClearSnoozeTime(string eventId)
        {
            Debug.WriteLine($"Clearing snooze time for event {eventId}");
            if (_snoozeTimers.Remove(eventId))
            {
                SnoozeTimeUpdated?.Invoke(this, eventId);
            }
        }

        public DateTime? GetSnoozeTime(string eventId)
        {
            if (_snoozeTimers.TryGetValue(eventId, out DateTime snoozeTime))
            {
                if (snoozeTime <= DateTime.Now && snoozeTime != DateTime.MaxValue)
                {
                    Debug.WriteLine($"Removing expired snooze time for event {eventId}");
                    _snoozeTimers.Remove(eventId);
                    return null;
                }
                return snoozeTime;
            }
            return null;
        }

        public void ClearExpiredSnoozeTimes()
        {
            Debug.WriteLine("Clearing expired snooze times");
            var expiredIds = _snoozeTimers
                .Where(kvp => kvp.Value <= DateTime.Now && kvp.Value != DateTime.MaxValue)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var id in expiredIds)
            {
                _snoozeTimers.Remove(id);
                SnoozeTimeUpdated?.Invoke(this, id);
            }
        }

        public void SubscribeToSnoozeUpdates(EventHandler<string> handler)
        {
            SnoozeTimeUpdated += handler;
        }

        public void UnsubscribeFromSnoozeUpdates(EventHandler<string> handler)
        {
            SnoozeTimeUpdated -= handler;
        }
    }
}