using Attendance.Data;

namespace Attendance.ViewModels;

public class RecurringEventViewModel
{
    readonly RecurringEvent _event;

    public RecurringEventViewModel(RecurringEvent recurringEvent)
    {
        _event = recurringEvent;
    }

    public int Id => _event.Id;

    public DateTime EndDate { get; set; } = DateTime.Today;

    public string FrequencyDescription
    {
        get
        {
            string dayOfWeek = _event.DayOfWeek.ToString();
            string ending = "";
            if (_event.EndDate.HasValue)
                ending = $", ended on {_event.EndDate:d}";

            return _event.Frequency switch {
                EventFrequency.EveryWeek => $"Every {dayOfWeek}, starting {_event.StartDate:d}{ending}",
                EventFrequency.FirstWeek => $"On the first {dayOfWeek} of the month, starting {_event.StartDate:d}{ending}",
                EventFrequency.SecondWeek => $"On the second {dayOfWeek} of the month, starting {_event.StartDate:d}{ending}",
                EventFrequency.ThirdWeek => $"On the third {dayOfWeek} of the month, starting {_event.StartDate:d}{ending}",
                EventFrequency.FourthWeek => $"On the fourth {dayOfWeek} of the month, starting  {_event.StartDate:d}{ending}",
                _ => "Unknown frequency"
            };
        }
    }

    public EventFrequency Frequency
    {
        get => _event.Frequency;
        set => _event.Frequency = value;
    }

    public DayOfWeek DayOfWeek
    {
        get => _event.DayOfWeek;
        set => _event.DayOfWeek = value;
    }

    public string Description
    {
        get => _event.Description ?? string.Empty;
        set => _event.Description = value;
    }

    public bool CanDelete => _event.StartDate > DateOnly.FromDateTime(DateTime.Today);
    public bool IsDeleted => _event.EndDate.HasValue;
}
