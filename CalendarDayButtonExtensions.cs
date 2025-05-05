using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace GoogleCalendarNotifier
{
    public static class CalendarDayButtonExtensions
    {
        public static readonly DependencyProperty HasEventsProperty =
            DependencyProperty.RegisterAttached(
                "HasEvents",
                typeof(bool),
                typeof(CalendarDayButtonExtensions),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsCurrentDayProperty =
            DependencyProperty.RegisterAttached(
                "IsCurrentDay",
                typeof(bool),
                typeof(CalendarDayButtonExtensions),
                new PropertyMetadata(false));
                
        public static readonly DependencyProperty IsOtherMonthProperty =
            DependencyProperty.RegisterAttached(
                "IsOtherMonth",
                typeof(bool),
                typeof(CalendarDayButtonExtensions),
                new PropertyMetadata(false));

        public static void SetHasEvents(CalendarDayButton element, bool value)
        {
            element.SetValue(HasEventsProperty, value);
        }

        public static bool GetHasEvents(CalendarDayButton element)
        {
            return (bool)element.GetValue(HasEventsProperty);
        }

        public static void SetIsCurrentDay(CalendarDayButton element, bool value)
        {
            element.SetValue(IsCurrentDayProperty, value);
        }

        public static bool GetIsCurrentDay(CalendarDayButton element)
        {
            return (bool)element.GetValue(IsCurrentDayProperty);
        }

        public static void SetIsOtherMonth(CalendarDayButton element, bool value)
        {
            element.SetValue(IsOtherMonthProperty, value);
        }

        public static bool GetIsOtherMonth(CalendarDayButton element)
        {
            return (bool)element.GetValue(IsOtherMonthProperty);
        }
    }
}