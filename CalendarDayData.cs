using System;
using System.Windows;

namespace GoogleCalendarNotifier
{
    public class CalendarDayData : DependencyObject
    {
        public static readonly DependencyProperty HasEventsProperty =
            DependencyProperty.Register(nameof(HasEvents), typeof(bool), typeof(CalendarDayData), new PropertyMetadata(false));

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(nameof(Date), typeof(DateTime), typeof(CalendarDayData), new PropertyMetadata(DateTime.MinValue));

        public bool HasEvents
        {
            get { return (bool)GetValue(HasEventsProperty); }
            set { SetValue(HasEventsProperty, value); }
        }

        public DateTime Date
        {
            get { return (DateTime)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }
    }
}
