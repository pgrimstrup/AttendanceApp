using Attendance.Data;

namespace Attendance.ViewModels;

public class AwayEventViewModel
{
    readonly AwayEvent _event;

    public AwayEventViewModel(AwayEvent? e = null)
    {
        _event = e ?? new AwayEvent();
    }

    public AwayEvent Entity => _event;

    public string Location
    {
        get => _event.Location;
        set => _event.Location = value;
    }

    public string EventName
    {
        get => _event.EventName;
        set => _event.EventName = value;
    }

    public string Notes
    {
        get => _event.Notes ?? "";
        set => _event.Notes = value;
    }

    public DateTime StartDate
    {
        get => _event.StartDate.ToDateTime(TimeOnly.MinValue);
        set
        {
            _event.StartDate = DateOnly.FromDateTime(value);
            if (_event.EndDate < _event.StartDate)
                _event.EndDate = _event.StartDate;
        }
    }

    public DateTime EndDate
    {
        get => _event.EndDate.ToDateTime(TimeOnly.MinValue);
        set
        {
            _event.EndDate = DateOnly.FromDateTime(value);
            if (_event.StartDate > _event.EndDate)
                _event.StartDate = _event.EndDate;
        }
    }
}
