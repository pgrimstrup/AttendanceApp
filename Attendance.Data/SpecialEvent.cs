namespace Attendance.Data;

public class SpecialEvent
{
    public int Id { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Description { get; set; } 
}
