using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarNotifier
{
    public class SettingsManager
    {
        public NotificationSettings NotificationSettings { get; set; } = new();
        public CalendarSettings CalendarSettings { get; set; } = new();

        public void LoadSettings()
        {
            // Load from configuration file
        }
    }
}
