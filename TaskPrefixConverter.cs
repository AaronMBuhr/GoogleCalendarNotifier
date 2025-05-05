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
                // If parameter is provided and it contains a boolean (IsCompleted)
                if (parameter is bool isCompleted && isCompleted)
                {
                    return "✅ "; // Checked checkbox symbol for completed tasks
                }
                
                return "⬜ "; // Unchecked checkbox symbol for uncompleted tasks
            }
            
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 