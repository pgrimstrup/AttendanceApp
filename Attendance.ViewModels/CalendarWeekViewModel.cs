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
    public int SpecialEvents => _day.SpecialEvents;
    public int RecurringEvents => _day.RecurringEvents;

    public CalendarDayViewModel(int year, int month, CalendarDay day)
    {
        _day = day;
        _year = year;
        _month = month;
    }


    public string Description
    {
        get
        {
            string desc = "";
            if (SpecialEvents > 0)
            {
                desc += $"{SpecialEvents} special event" + (SpecialEvents > 1 ? "s" : "");
                if (RecurringEvents > 0)
                    desc += " and ";
            }

            if (RecurringEvents > 0)
                desc += $"{RecurringEvents} recurring event" + (RecurringEvents > 1 ? "s" : "");

            if (_day.CancelEvents)
                desc += " (cancelled)";

            return desc;
        }
    }

    public bool IsChecked
    {
        get => (SpecialEvents > 0 || RecurringEvents > 0) && !_day.CancelEvents;
        set
        {
            _day.CancelEvents = !value;
        }
    }

    public bool IsDisabled
    {
        get => SpecialEvents == 0 && RecurringEvents == 0;
    }

    public bool IsCancelled => _day.CancelEvents;

    public bool IsCurrentMonth => Date.Year == _year && Date.Month == _month;
}
