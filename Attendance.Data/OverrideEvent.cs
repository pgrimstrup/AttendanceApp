namespace Attendance.Data;

public class OverrideEvent
{
    public int PersonId { get; set; }
    public DateOnly EventDate { get; set; }
    public bool IsIncluded { get; set; }
    public bool IsExcluded { get; set; }
}
