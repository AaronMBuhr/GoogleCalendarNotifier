using System;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCalendarNotifier
{
    public class TimeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAllDay)
            {
                return isAllDay ? "All Day" : "{0:HH:mm}";
            }
            return "{0:HH:mm}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}