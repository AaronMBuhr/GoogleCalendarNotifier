using System.Text;

namespace GoogleCalendarNotifier
{
    public partial class MainForm : Form
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly CalendarMonitorService _monitorService;
        private readonly SettingsManager _settingsManager;
        private readonly System.Windows.Forms.Timer _checkTimer;
        private TextBox txtResults;
        private Button btnTest;

        public MainForm(IGoogleCalendarService calendarService, CalendarMonitorService monitorService, SettingsManager settingsManager)
        {
            InitializeComponent();
            _calendarService = calendarService;
            _monitorService = monitorService;
            _settingsManager = settingsManager;
            _checkTimer = new System.Windows.Forms.Timer();

            InitializeTestControls();
        }

        private void InitializeTestControls()
        {
            // Set form properties
            this.Text = "Google Calendar Notifier";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ClientSize = new Size(584, 362);

            // Create and configure the results textbox first
            txtResults = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(12, 12),
                Size = new Size(560, 300),
                ReadOnly = true
            };

            // Create and configure the test button at the bottom
            btnTest = new Button
            {
                Text = "Test Calendar Connection",
                Location = new Point(12, txtResults.Bottom + 10),
                Size = new Size(150, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnTest.Click += BtnTest_Click;

            // Add controls to the form
            Controls.Add(txtResults);
            Controls.Add(btnTest);
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            try
            {
                btnTest.Enabled = false;
                txtResults.Text = "Initializing calendar service...";

                await _calendarService.InitializeAsync();

                txtResults.Text = "Fetching upcoming events...";
                var events = await _calendarService.GetUpcomingEventsAsync(TimeSpan.FromDays(7));

                var sb = new StringBuilder();
                sb.AppendLine($"Successfully fetched {events.Count()} events for the next 7 days:");
                sb.AppendLine();

                foreach (var evt in events)
                {
                    sb.AppendLine($"Title: {evt.Title}");
                    sb.AppendLine($"Start: {evt.StartTime:g}");
                    sb.AppendLine($"All Day: {evt.IsAllDay}");
                    if (evt.ReminderTime.HasValue)
                    {
                        sb.AppendLine($"Reminder: {evt.ReminderTime.Value.TotalMinutes} minutes before");
                    }
                    sb.AppendLine();
                }

                txtResults.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                txtResults.Text = $"Error: {ex.Message}\r\n\r\nStack Trace:\r\n{ex.StackTrace}";
            }
            finally
            {
                btnTest.Enabled = true;
            }
        }
    }
}
