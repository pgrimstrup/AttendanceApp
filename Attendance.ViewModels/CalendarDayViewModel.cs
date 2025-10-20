using Attendance.Data;

namespace Attendance.ViewModels;

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

    public CalendarDay Entity => _day;

    public bool IsChecked => (_day.IsPistolDay && !_day.IsClosedDay && !_day.IsClosedOverride) || (_day.IsOpenOverride);
    public bool IsClosedOverride => _day.IsClosedOverride;

    public bool IsDisabled
    {
        get => _day.Events.Count == 0 || !_day.IsPistolDay;
    }

    public bool IsCurrentMonth => Date.Year == _year && Date.Month == _month;

    public IEnumerable<CalendarEntry> Events => _day.Events;
}
