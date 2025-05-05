using System;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCalendarNotifier
{
    public class TaskPrefixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTask && isTask)
            {
                return "⬜ "; // Checkbox symbol for tasks
            }
            
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 