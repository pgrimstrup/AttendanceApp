namespace Attendance.Data;

public enum EventFrequency
{
    EveryWeek,
    FirstWeek,
    SecondWeek,
    ThirdWeek,
    FourthWeek,
}


public class RecurringEvent
{
    public int Id { get; set; }
    public EventFrequency Frequency { get; set; }
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
}
