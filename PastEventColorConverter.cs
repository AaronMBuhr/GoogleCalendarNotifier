using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GoogleCalendarNotifier
{
    public class PastEventColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime eventDate)
            {
                // Check if the event date is earlier than today
                DateTime today = DateTime.Today;
                
                if (eventDate.Date < today)
                {
                    // Return a predefined DarkGray brush for past events
                    return System.Windows.Media.Brushes.DarkGray;
                }
            }
            
            // For current or future events, return the default MahApps text brush (which seems to be working)
            return System.Windows.Application.Current.Resources["MahApps.Brushes.Text"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter doesn't support two-way binding
            throw new NotImplementedException();
        }
    }
} 