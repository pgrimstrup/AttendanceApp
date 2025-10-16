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

public class CalendarDayViewModel
{
    readonly CalendarDay _day;
    int _year;
    int _month;
    public DateTime Date => _day.Date.ToDateTime(new TimeOnly(0, 0));

    public CalendarDayViewModel(int year, int month, CalendarDay day)
    {
        _day = day;
        _year = year;
        _month = month;
    }


    public bool IsChecked
    {
        get => _day.IsPistolDay && !_day.IsClosedDay;
    }

    public bool IsDisabled
    {
        get => _day.Events.Count == 0 || !_day.IsPistolDay;
    }

    public bool IsClosed
    {
        get => _day.IsClosedDay;
        set => _day.IsClosedDay = value;
    }

    public bool IsCurrentMonth => Date.Year == _year && Date.Month == _month;

    public IEnumerable<CalendarEntry> Events => _day.Events;
}
