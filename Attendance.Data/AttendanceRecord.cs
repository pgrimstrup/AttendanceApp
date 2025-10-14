namespace Attendance.Data;

public record AttendanceRecord
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RegisteredCardNumber { get; set; } = string.Empty;
    public string FALNumber { get; set; } = string.Empty;
    public string PNZNumber { get; set; } = string.Empty;
    public string Sections { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public bool IsMatchDay { get; set; }
    public DateTime? EntryTime { get; set; }
    public DateTime? RangeTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public string SwipedCardNumber { get; set; } = string.Empty;
    public string SwipedPersonId { get; set; } = string.Empty;
}
