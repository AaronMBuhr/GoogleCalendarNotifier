using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GoogleCalendarNotifier
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If parameter is provided and is "Invert", invert the logic
                bool invert = parameter is string paramString && paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase);
                
                // Apply inversion if needed
                bool result = invert ? !boolValue : boolValue;
                
                // Convert to Visibility
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // If parameter is provided and is "Invert", invert the logic
                bool invert = parameter is string paramString && paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase);
                
                // Convert Visibility to bool with possible inversion
                bool result = visibility == Visibility.Visible;
                return invert ? !result : result;
            }
            
            return false;
        }
    }
} 