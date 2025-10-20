using Attendance.Data;

namespace Attendance.ViewModels;

public class CalendarWeekViewModel
{
    public List<CalendarDayViewModel> Days { get; } = new();

    public CalendarWeekViewModel(int year, int month, CalendarDay[] days)
    {
        Days.AddRange(days.Select(d => new CalendarDayViewModel(year, month, d)));
    }
}


