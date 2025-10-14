namespace Attendance.Data;

public record AttendanceRecord
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RegisteredCardNumber { get; set; } = string.Empty;
    public string FALNumber { get; set; } = string.Empty;
    public string PNZNumber { get; set; } = string.Empty;
    public SectionTags Sections { get; set; } 
    public DateTime EventDate { get; set; }
    public bool IsMatchDay { get; set; }
    public bool IsAwayEvent { get; set; }
    public bool IsExcluded { get; set; }
    public bool IsIncluded { get; set; }
    public DateTime? EntryTime { get; set; }
    public DateTime? RangeTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public string? AwayEventName { get; set; }
    public string? AwayEventLocation { get; set; }
    public string SwipedCardNumber { get; set; } = string.Empty;
    public int SwipedPersonId { get; set; }

    public bool IsPistolSection => 
        Sections.HasFlag(SectionTags.CAS) || 
        Sections.HasFlag(SectionTags.IPSC) || 
        Sections.HasFlag(SectionTags.ISSF) || 
        Sections.HasFlag(SectionTags.MultiGun);

}
