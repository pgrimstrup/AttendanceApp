using Attendance.Data;

namespace Attendance.ViewModels;

public class SpecialEventViewModel
{
    readonly SpecialEvent _event;

    public SpecialEventViewModel(SpecialEvent specialEvent)
    {
        _event = specialEvent;
    }
    
    public int Id => _event.Id;

    public string Duration
    {
        get
        {
            int days = (_event.EndDate.ToDateTime(TimeOnly.MinValue) - _event.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
            if(days <= 1)
                return $"{_event.StartDate:d} ({_event.StartDate:dddd})";

            return $"{_event.StartDate:d} to {_event.EndDate:d} ({days} days, {_event.StartDate:dddd} to {_event.EndDate:dddd})";
        }
    }

    public DateTime StartDate
    {
        get => _event.StartDate.ToDateTime(TimeOnly.MinValue);
        set => _event.StartDate = DateOnly.FromDateTime(value);
    }

    public DateTime EndDate
    {
        get => _event.EndDate.ToDateTime(TimeOnly.MinValue);
        set => _event.EndDate = DateOnly.FromDateTime(value);
    }

    public string Description
    {
        get => _event.Description ?? string.Empty;
        set => _event.Description = value;
    }

}
