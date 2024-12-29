# Google Calendar Notifier

A Windows application that provides enhanced notifications for Google Calendar events, featuring obstructive notifications that cannot be missed and flexible snooze options.

## Features

### Calendar Integration
- Reads events up to 90 days in advance
- Interactive calendar with event highlighting
- Real-time event display with automatic refresh every 5 minutes
- Manual refresh option

### Event Display
- Calendar control highlights current day and days with events
- Event table showing event names and times
- Interactive selection between calendar and event list
- Detailed event information display
- Snooze status tooltips

### Notifications
- Supports Google Calendar's native reminder times
- Falls back to event start time when no reminder is set
- Handles multiple notification times per event
- Support for all-day events
- Flexible snooze options:
  - 5 minutes
  - 15 minutes
  - 30 minutes
  - 60 minutes
  - 1 day
  - Never (disable notifications)
  - Event Time (reset to original time)
- Recurring event support with unified snooze settings

### System Integration
- System tray icon for quick access
- Optional Windows startup integration
- Secure credential storage using Windows DPAPI
- Configuration stored in user's roaming AppData

### Reliability
- Offline support with cached data
- Automatic connection retry
- Missed notification recovery on startup
- Automatic expired snooze cleanup

## Setup

1. Create a Google Cloud Project
2. Set up OAuth consent screen
3. Create Desktop app OAuth credentials
4. Download credentials.json to the project directory
5. Add your Google account as a test user in the Google Cloud Console
6. Run the application

## Development Status

The application is fully functional with all core features implemented. Future updates will focus on configuration options and user experience improvements.

## Technical Details

### Configuration
- Settings stored in YAML format
- Location: %AppData%\Roaming\AaronsWorld\GoogleCalendarNotifier
- Configurable parameters:
  - Calendar read-ahead days
  - Refresh interval
  - Snooze times

### Security
- Credentials secured using Windows DPAPI
- Sensitive configuration data encrypted
- OAuth refresh tokens stored securely

## Note for Development Testing
During test mode in the Google Cloud Console, credentials must be refreshed at each application start.