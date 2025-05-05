using System;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCalendarNotifier
{
    public class TaskStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if we have all required values
            if (values.Length >= 3 && 
                values[0] is bool isTask && 
                values[1] is bool isCompleted && 
                values[2] is string title)
            {
                string prefix = "";
                
                // Add a prefix if it's a task
                if (isTask)
                {
                    prefix = isCompleted ? "✅ " : "⬜ ";
                }
                
                // Return the combined prefix + title
                return prefix + title;
            }
            
            // If we don't have all values or they're of wrong type, just return the third one (title) if it exists
            if (values.Length >= 3 && values[2] is string fallbackTitle)
            {
                return fallbackTitle;
            }
            
            // Fallback
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 