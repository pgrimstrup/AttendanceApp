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

    public bool IsPistolDay { get; set; }
    public bool IsClosedDay { get; set; }
    public bool IsClosedOverride { get; set; }
    public bool IsOpenOverride { get; set; }
    public bool IsAwayEvent { get; set; }
    public bool IsExcluded { get; set; }
    public bool IsIncluded { get; set; }
    public string? AwayEventName { get; set; }
    public string? AwayLocation { get; set; }

    public DateTime? EntryTime { get; set; }
    public DateTime? RangeTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public string SwipedCardNumber { get; set; } = string.Empty;

    public bool IsMatchDay
    {
        get
        {
            // Away events are always considered match days
            if (IsAwayEvent)
                return true;

            if (!IsClosedOverride)
            {
                if (IsOpenOverride)
                    return true;

                if (IsPistolDay && !IsClosedDay)
                    return true;
            }

            return false;
        }
    }

    public bool IsCounted
    {
        get
        {
            // Away events are always counted
            if (IsAwayEvent)
                return true;

            if (IsMatchDay)
            {
                // Check for overrides first
                if (IsIncluded)
                    return true;

                if (IsExcluded)
                    return false;

                return RangeTime.HasValue;
            }

            return false;
        }
    }
}
