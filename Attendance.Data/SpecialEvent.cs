namespace Attendance.Data;

public class SpecialEvent
{
    public int Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Description { get; set; } 
}
