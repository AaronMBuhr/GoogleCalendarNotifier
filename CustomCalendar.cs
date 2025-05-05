using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media;

namespace GoogleCalendarNotifier
{
    public class CustomCalendar : Calendar
    {
        private HashSet<DateTime> datesWithEvents = new HashSet<DateTime>();
        private HashSet<DateTime> datesWithTasks = new HashSet<DateTime>();
        private HashSet<DateTime> datesWithHolidays = new HashSet<DateTime>();
        private DateTime? lastSelectedDate;

        public CustomCalendar()
        {
            Debug.WriteLine($"CustomCalendar: Constructor called");
            
            // Track our own selection changes
            this.SelectedDatesChanged += CustomCalendar_SelectedDatesChanged;
            
            // Initialize with today
            var today = DateTime.Today;
            Debug.WriteLine($"CustomCalendar: Setting initial date to {today}");
            SelectedDate = today;
            DisplayDate = today;

            this.PreviewMouseDown += CustomCalendar_PreviewMouseDown;
            this.Loaded += CustomCalendar_Loaded;
        }

        private void CustomCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("CustomCalendar: Loaded event fired");
            UpdateCalendarDayButtons();
        }

        private void CustomCalendar_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource;
            Debug.WriteLine($"CustomCalendar: PreviewMouseDown on {originalSource.GetType()}");

            if (originalSource is CalendarDayButton dayButton)
            {
                if (dayButton.DataContext is DateTime clickedDate)
                {
                    Debug.WriteLine($"CustomCalendar: Day button clicked for date {clickedDate:d}");
                }
            }
        }

        private void CustomCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("CustomCalendar: SelectedDatesChanged");
            if (e.RemovedItems.Count > 0)
                Debug.WriteLine($"  Removed: {string.Join(", ", e.RemovedItems.Cast<DateTime>().Select(d => d.ToString("d")))}");
            if (e.AddedItems.Count > 0)
                Debug.WriteLine($"  Added: {string.Join(", ", e.AddedItems.Cast<DateTime>().Select(d => d.ToString("d")))}");

            lastSelectedDate = SelectedDate;
            UpdateCalendarDayButtons();
        }

        protected override void OnDisplayDateChanged(CalendarDateChangedEventArgs e)
        {
            Debug.WriteLine($"CustomCalendar: OnDisplayDateChanged - Old: {e.RemovedDate:d}, New: {e.AddedDate:d}");
            base.OnDisplayDateChanged(e);
            UpdateCalendarDayButtons();
        }

        protected override void OnSelectedDatesChanged(SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"CustomCalendar: OnSelectedDatesChanged - Base class");
            base.OnSelectedDatesChanged(e);
        }

        public void SetDatesWithEvents(IEnumerable<DateTime> dates)
        {
            datesWithEvents = new HashSet<DateTime>(dates.Select(d => d.Date));
            Debug.WriteLine($"CustomCalendar: SetDatesWithEvents - Count: {datesWithEvents.Count}");
            Debug.WriteLine($"CustomCalendar: Event dates - {string.Join(", ", datesWithEvents.Select(d => d.ToString("d")))}");
            UpdateCalendarDayButtons();
        }
        
        public void SetDatesWithTasks(IEnumerable<DateTime> dates)
        {
            datesWithTasks = new HashSet<DateTime>(dates.Select(d => d.Date));
            Debug.WriteLine($"CustomCalendar: SetDatesWithTasks - Count: {datesWithTasks.Count}");
            Debug.WriteLine($"CustomCalendar: Task dates - {string.Join(", ", datesWithTasks.Select(d => d.ToString("d")))}");
            UpdateCalendarDayButtons();
        }
        
        public void SetDatesWithHolidays(IEnumerable<DateTime> dates)
        {
            datesWithHolidays = new HashSet<DateTime>(dates.Select(d => d.Date));
            Debug.WriteLine($"CustomCalendar: SetDatesWithHolidays - Count: {datesWithHolidays.Count}");
            Debug.WriteLine($"CustomCalendar: Holiday dates - {string.Join(", ", datesWithHolidays.Select(d => d.ToString("d")))}");
            UpdateCalendarDayButtons();
        }

        private void UpdateCalendarDayButtons()
        {
            Debug.WriteLine("CustomCalendar: UpdateCalendarDayButtons called");
            
            var dayButtons = FindVisualChildren<CalendarDayButton>(this).ToList();
            Debug.WriteLine($"  Found {dayButtons.Count} day buttons");

            foreach (var dayButton in dayButtons)
            {
                if (dayButton.DataContext is DateTime date)
                {
                    UpdateDayButtonState(dayButton, date);
                }
            }
        }

        private void UpdateDayButtonState(CalendarDayButton button, DateTime date)
        {
            var isToday = date.Date == DateTime.Today;
            var isSelected = date.Date == SelectedDate?.Date;
            bool isInCurrentMonth = date.Month == DisplayDate.Month && date.Year == DisplayDate.Year;
            
            Debug.WriteLine($"UpdateDayButtonState: Date={date:d}, Today={isToday}, Selected={isSelected}, CurrentMonth={isInCurrentMonth}");

            // Set HasEvents property
            var hasEvents = datesWithEvents.Contains(date.Date);
            CalendarDayButtonExtensions.SetHasEvents(button, hasEvents);
            
            // Set HasTasks property
            var hasTasks = datesWithTasks.Contains(date.Date);
            CalendarDayButtonExtensions.SetHasTasks(button, hasTasks);
            
            // Set HasHolidays property
            var hasHolidays = datesWithHolidays.Contains(date.Date);
            CalendarDayButtonExtensions.SetHasHolidays(button, hasHolidays);

            // Set IsCurrentDay property
            CalendarDayButtonExtensions.SetIsCurrentDay(button, isToday);

            // Set IsOtherMonth property
            CalendarDayButtonExtensions.SetIsOtherMonth(button, !isInCurrentMonth);
        }

        public override void OnApplyTemplate()
        {
            Debug.WriteLine("CustomCalendar: OnApplyTemplate called");
            base.OnApplyTemplate();
            UpdateCalendarDayButtons();
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}