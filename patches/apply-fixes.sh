#!/bin/bash
set -e  # Exit on error

# Set up logging with tee
exec 1> >(tee -a "patch-fixes.log")
exec 2> >(tee -a "patch-fixes.log" >&2)

echo "Starting modifications at $(date)"
echo "============================="

# Convert Windows path to WSL path if needed
PROJ_DIR=$(wslpath -u "E:/Source/Mine/GoogleCalendarNotifier")
cd "$PROJ_DIR"

echo "Fixing MainWindow.xaml NotifyIcon references..."
sed -i 's/TrayMouseDoubleClick="NotifyIcon_TrayMouseDoubleClick"/MouseDoubleClick="NotifyIcon_TrayMouseDoubleClick"/' MainWindow.xaml

echo "Fixing obsolete TimeMin/TimeMax in GoogleCalendarService.cs..."
sed -i 's/request\.TimeMin = now;/request.TimeMinDateTimeOffset = new DateTimeOffset(now);/' GoogleCalendarService.cs
sed -i 's/request\.TimeMax = now\.Add(lookAheadTime);/request.TimeMaxDateTimeOffset = new DateTimeOffset(now.Add(lookAheadTime));/' GoogleCalendarService.cs

echo "Fixing obsolete DateTime properties in GoogleCalendarService.cs..."
sed -i 's/item\.Start\.DateTime/item.Start.DateTimeDateTimeOffset/g' GoogleCalendarService.cs
sed -i 's/item\.End\.DateTime/item.End.DateTimeDateTimeOffset/g' GoogleCalendarService.cs

echo "Fixing WindowsNotificationService.cs constructor and field issues..."
# Create temporary file
cat > WindowsNotificationService.cs.tmp << 'EOL'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using H.NotifyIcon;
using Microsoft.Toolkit.Uwp.Notifications;

namespace GoogleCalendarNotifier
{
    public class WindowsNotificationService : INotificationService
    {
        private readonly TaskbarIcon _trayIcon;
        private readonly NotificationSettings _settings;

        public WindowsNotificationService()
        {
            _trayIcon = new TaskbarIcon();
            _settings = new NotificationSettings();
        }

        void INotificationService.ShowNotification(string title, string message, NotificationType type)
        {
            // Use Windows Toast Notifications
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(message);

            // Adjust the notification based on type
            switch (type)
            {
                case NotificationType.Warning:
                    builder.AddAttributionText("Warning");
                    _trayIcon.ToolTipText = "Warning: " + message;
                    break;
                case NotificationType.Error:
                    builder.AddAttributionText("Error");
                    _trayIcon.ToolTipText = "Error: " + message;
                    break;
                case NotificationType.Success:
                    builder.AddAttributionText("Success");
                    _trayIcon.ToolTipText = "Success: " + message;
                    break;
                default:
                    _trayIcon.ToolTipText = message;
                    break;
            }

            builder.Show();
        }

        void INotificationService.Configure(NotificationSettings settings)
        {
            // Update any configurable settings here
        }

        ~WindowsNotificationService()
        {
            _trayIcon.Dispose();
        }
    }
}
EOL

mv WindowsNotificationService.cs.tmp WindowsNotificationService.cs

echo "============================="
echo "Completed modifications at $(date)"
