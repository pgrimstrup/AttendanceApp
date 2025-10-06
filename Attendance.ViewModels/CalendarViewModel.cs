namespace Attendance.ViewModels;

public class CalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }

    public List<CalendarWeekViewModel> Weeks { get; } = new();
    public List<RecurringEventViewModel> RecurringEvents { get; } = new();
    public List<SpecialEventViewModel> SpecialEvents { get; } = new();

    public DateTime CurrentMonth => new DateTime(Year, Month, 1);
    public DateTime NextMonth => CurrentMonth.AddMonths(1);
    public DateTime PrevMonth => CurrentMonth.AddMonths(-1);

    public bool IsPrevMonthDisabled => (Year == 2025 && Month <= 7) || (Year < 2025);


    public CalendarViewModel(int? year, int? month)
    {
        Year = year ?? DateTime.Today.Year;
        Month = month ?? DateTime.Today.Month;

        // Bounds checking the calendar
        int minYear = 2025;
        int maxYear = DateTime.Today.Month < 7 ? DateTime.Today.Year + 1 : DateTime.Today.Year + 2;
        if (Year < minYear || (Year == minYear && Month < 7))
        {
            Year = minYear;
            Month = 7;
        }

        if (Year > maxYear || (Year == maxYear && Month > 6))
        {
            Year = maxYear;
            Month = 6;
        }
    }

    public bool IsNextMonthDisabled
    {
        get
        {
            // Can only go forward to the end of the next calendar year
            if (DateTime.Today.Month <= 6)
                return (Year == DateTime.Today.Year + 1 && Month >= 6) || (Year > DateTime.Today.Year + 1);
            else
                return (Year == DateTime.Today.Year + 2 && Month >= 6) || (Year > DateTime.Today.Year + 2);
        }
    }
}
