using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Diagnostics;

namespace GoogleCalendarNotifier
{
    public partial class NotificationPopup : Window
    {
        public event Action<TimeSpan?> OnSnooze;
        public event Action OnDismiss;

        public NotificationPopup()
        {
            InitializeComponent();
            Loaded += NotificationPopup_Loaded;
            
            // Position window in bottom right corner
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            PositionWindow();
        }

        private void PositionWindow()
        {
            var screenWidth = SystemParameters.WorkArea.Width;
            var screenHeight = SystemParameters.WorkArea.Height;
            this.Left = screenWidth - this.Width - 20;
            this.Top = screenHeight - this.Height - 20;
        }

        private void NotificationPopup_Loaded(object sender, RoutedEventArgs e)
        {
            // Fade in animation
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        public void ShowNotification(string title, string message)
        {
            TitleText.Text = title;
            MessageText.Text = message;
            Show();
            Activate();
        }

        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = SnoozeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            TimeSpan? snoozeTime = null;
            string option = selectedItem.Content.ToString();
            Debug.WriteLine($"Selected snooze option: {option}");

            switch (option)
            {
                case "5 minutes":
                    snoozeTime = TimeSpan.FromMinutes(5);
                    break;
                case "15 minutes":
                    snoozeTime = TimeSpan.FromMinutes(15);
                    break;
                case "30 minutes":
                    snoozeTime = TimeSpan.FromMinutes(30);
                    break;
                case "60 minutes":
                    snoozeTime = TimeSpan.FromMinutes(60);
                    break;
                case "1 day":
                    snoozeTime = TimeSpan.FromDays(1);
                    break;
                case "Never":
                    snoozeTime = TimeSpan.MaxValue;
                    Debug.WriteLine("Setting snooze to Never (MaxValue)");
                    break;
                case "Event Time":
                    snoozeTime = TimeSpan.Zero;
                    Debug.WriteLine("Setting snooze to Event Time (Zero)");
                    break;
            }

            Debug.WriteLine($"Invoking OnSnooze with value: {snoozeTime}");
            OnSnooze?.Invoke(snoozeTime);
            Close();
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            OnDismiss?.Invoke();
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}