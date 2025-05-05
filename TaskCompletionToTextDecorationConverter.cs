using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

namespace GoogleCalendarNotifier
{
    public class TaskCompletionToTextDecorationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if this is a task and it's completed
            if (values.Length >= 2 && values[0] is bool isTask && values[1] is bool isCompleted)
            {
                if (isTask && isCompleted)
                {
                    // Return a strikethrough text decoration
                    return TextDecorations.Strikethrough;
                }
            }
            
            // Default - no decoration
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 