using System;

namespace GoogleCalendarNotifier
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Application Error: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}