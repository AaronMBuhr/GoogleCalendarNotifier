using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Diagnostics;

namespace GoogleCalendarNotifier
{
    public class ConfigManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AaronsWorld",
            "GoogleCalendarNotifier");

        private static readonly string ConfigPath = Path.Combine(AppDataPath, "config.yaml");
        
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;
        private AppConfig _config;

        public ConfigManager()
        {
            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _config = LoadConfig();
        }

        public AppConfig Config => _config;

        private AppConfig LoadConfig()
        {
            try
            {
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }

                if (!File.Exists(ConfigPath))
                {
                    return new AppConfig();
                }

                var yaml = File.ReadAllText(ConfigPath);
                return _deserializer.Deserialize<AppConfig>(yaml) ?? new AppConfig();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading config: {ex.Message}");
                return new AppConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                var yaml = _serializer.Serialize(_config);
                File.WriteAllText(ConfigPath, yaml);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        // Helper methods for common operations
        public void SnoozeEvent(string eventId, DateTime until, DateTime originalNotificationTime)
        {
            _config.State.SnoozedEvents[eventId] = new SnoozeInfo
            {
                EventId = eventId,
                SnoozeUntil = until,
                OriginalNotificationTime = originalNotificationTime
            };
            SaveConfig();
        }

        public void SetCustomNotification(string eventId, DateTime notificationTime, TimeSpan? repeatInterval = null)
        {
            _config.State.CustomNotifications[eventId] = new CustomNotification
            {
                EventId = eventId,
                NotificationTime = notificationTime,
                RepeatInterval = repeatInterval,
                IsEnabled = true
            };
            SaveConfig();
        }

        public void ClearSnooze(string eventId)
        {
            if (_config.State.SnoozedEvents.Remove(eventId))
            {
                SaveConfig();
            }
        }

        public void RemoveCustomNotification(string eventId)
        {
            if (_config.State.CustomNotifications.Remove(eventId))
            {
                SaveConfig();
            }
        }

        public bool IsEventSnoozed(string eventId, out DateTime snoozeUntil)
        {
            if (_config.State.SnoozedEvents.TryGetValue(eventId, out var snoozeInfo))
            {
                snoozeUntil = snoozeInfo.SnoozeUntil;
                return snoozeUntil > DateTime.Now;
            }
            snoozeUntil = DateTime.MinValue;
            return false;
        }

        public CustomNotification? GetCustomNotification(string eventId)
        {
            _config.State.CustomNotifications.TryGetValue(eventId, out var notification);
            return notification;
        }
    }
}