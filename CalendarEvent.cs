using System;
using System.ComponentModel;

namespace GoogleCalendarNotifier
{
    public class CalendarEvent : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAllDay { get; set; }
        public TimeSpan? ReminderTime { get; set; }
        public string CalendarId { get; set; } // ID of the source calendar
        public string CalendarName { get; set; } // Name of the source calendar
        public bool IsHoliday { get; set; } // Whether this is from a holiday calendar
        public bool IsTask { get; set; } // Whether this is a task instead of an event
        
        private bool _isHighlighted;
        public bool IsHighlighted 
        { 
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnPropertyChanged(nameof(IsHighlighted));
                }
            }
        }

        private DateTime? _snoozeUntil;
        public DateTime? SnoozeUntil
        {
            get => _snoozeUntil;
            set
            {
                if (_snoozeUntil != value)
                {
                    _snoozeUntil = value;
                    OnPropertyChanged(nameof(SnoozeUntil));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}