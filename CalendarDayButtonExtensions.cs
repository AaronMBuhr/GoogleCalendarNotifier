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

        public static void SetHasEvents(CalendarDayButton element, bool value)
        {
            element.SetValue(HasEventsProperty, value);
        }

        public static bool GetHasEvents(CalendarDayButton element)
        {
            return (bool)element.GetValue(HasEventsProperty);
        }
    }
}