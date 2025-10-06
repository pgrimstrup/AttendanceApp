using Attendance.Data;
using Attendance.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Attendance.Pages
{
    public class CalendarModel : PageModel
    {
        readonly ICalendarManager CalendarManager;

        public List<SpecialEventViewModel> SpecialEvents { get; }
        public List<RecurringEventViewModel> RecurringEvents { get; }

        public List<CalendarWeekViewModel> Weeks { get;  }

        [BindProperty]
        public int? Year { get; set; }
        [BindProperty]
        public int? Month { get; set; }

        public DateTime CurrentMonth => new DateTime(Year ?? DateTime.Today.Year, Month ?? DateTime.Today.Month, 1);
        public DateTime NextMonth => CurrentMonth.AddMonths(1);
        public DateTime PrevMonth => CurrentMonth.AddMonths(-1);

        public CalendarModel(ICalendarManager calendarManager)
        {
            CalendarManager = calendarManager;
            SpecialEvents = new ();
            RecurringEvents = new ();
            Weeks = new();
        }

        public async Task<IActionResult> OnGetAsync(int? year, int? month)
        {
            Year = year ?? DateTime.Today.Year;
            Month = month ?? DateTime.Today.Month;

            int minYear = 2025;
            int maxYear = DateTime.Today.Month < 7 ? DateTime.Today.Year + 1 : DateTime.Today.Year + 2;
            if(Year < minYear || (Year == minYear && Month < 7))
                return RedirectToPage(new { year = minYear, month = 7 });

            if (Year > maxYear || (Year == maxYear && Month > 6))
                return RedirectToPage(new { year = maxYear, month = 6 });

            var days = await CalendarManager.GetCalendarDays(Year.Value, Month.Value);
            for(int i = 0; i < days.Length; i += 7)
            {
                var week = new CalendarWeekViewModel(Year.Value, Month.Value, days.Skip(i).Take(7).ToArray());
                Weeks.Add(week);
            }

            var specialEvents = await CalendarManager.GetSpecialEvents();
            SpecialEvents.AddRange(specialEvents.Select(e => new SpecialEventViewModel(e)));

            var recurringEvents = await CalendarManager.GetRecurringEvents();
            RecurringEvents.AddRange(recurringEvents.Select(e => new RecurringEventViewModel(e)));
            return Page();
        }

        public async Task<IActionResult> OnPostAddSpecialEventAsync(DateTime startDate, DateTime endDate, string? description)
        {
            if (startDate > endDate)
            {
                ModelState.AddModelError(string.Empty, "Start date cannot be after the end date.");
                return Page();
            }

            var specialEvent = new SpecialEvent {
                StartDate = DateOnly.FromDateTime(startDate),
                EndDate = DateOnly.FromDateTime(endDate),
                Description = description
            };

            var result = await CalendarManager.AddOrUpdateSpecialEvent(specialEvent);
            if (!result)
                ModelState.AddModelError(string.Empty, "Failed to add the special event.");

            if(ModelState.IsValid)
                return RedirectToPage(new { year = Year, month = Month});
            return Page();
        }

        public async Task<IActionResult> OnPostAddRecurringEventAsync(DateTime startDate, EventFrequency frequency, DayOfWeek dayOfWeek, string? description)
        {
            var recurringEvent = new RecurringEvent {
                StartDate = DateOnly.FromDateTime(startDate),
                Frequency = frequency,
                DayOfWeek = dayOfWeek,
                Description = description
            };

            var result = await CalendarManager.AddOrUpdateRecurringEvent(recurringEvent);
            if (!result)
                ModelState.AddModelError(string.Empty, "Failed to add the recurring event.");

            if (ModelState.IsValid)
                return RedirectToPage(new { year = Year, month = Month });
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteSpecialEventAsync(int id)
        {
            var success = await CalendarManager.DeleteSpecialEvent(id);
            if(!success)
                ModelState.AddModelError(string.Empty, "Failed to delete the special event.");

            if (ModelState.IsValid)
                return RedirectToPage(new { year = Year, month = Month });
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteRecurringEventAsync(int id)
        {
            var success = await CalendarManager.DeleteRecurringEvent(id);
            if(!success)
                ModelState.AddModelError(string.Empty, "Failed to delete the recurring event.");    

            if (ModelState.IsValid)
                return RedirectToPage(new { year = Year, month = Month });
            return Page();
        }

        public async Task<IActionResult> OnPostEndRecurringEventAsync(int id, DateTime endDate)
        {
            var success = await CalendarManager.EndRecurringEvent(id, endDate);
            if (!success)
                ModelState.AddModelError(string.Empty, "Failed to delete the recurring event.");

            if (ModelState.IsValid)
                return RedirectToPage(new { year = Year, month = Month });
            return Page();
        }

        public async Task<IActionResult> OnPostResumeRecurringEventAsync(int id)
        {
            var success = await CalendarManager.ResumeRecurringEvent(id);
            if (!success)
                ModelState.AddModelError(string.Empty, "Failed to delete the recurring event.");

            if (ModelState.IsValid)
                return RedirectToPage(new { year = Year, month = Month });
            return Page();
        }
    }
}
