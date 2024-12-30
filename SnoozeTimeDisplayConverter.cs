using System;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCalendarNotifier
{
    public class SnoozeTimeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime snoozeTime)
            {
                if (snoozeTime == DateTime.MaxValue)
                    return "Never";
                
                if (snoozeTime < DateTime.Now)
                    return "";

                bool detailed = parameter as string == "detailed";
                if (detailed)
                {
                    return snoozeTime.ToString("MM/dd/yyyy HH:mm");
                }
                else
                {
                    var timeUntil = snoozeTime - DateTime.Now;
                    if (timeUntil.TotalDays >= 1)
                        return $"In {(int)timeUntil.TotalDays}d {timeUntil.Hours}h";
                    else if (timeUntil.TotalHours >= 1)
                        return $"In {(int)timeUntil.TotalHours}h {timeUntil.Minutes}m";
                    else
                        return $"In {timeUntil.Minutes}m";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}