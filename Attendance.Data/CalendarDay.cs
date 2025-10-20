using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Attendance.Data;


public class CalendarDay
{
    public DateOnly Date { get; set; }
    public bool IsClosedDay { get; set; }
    public bool IsPistolDay { get; set; }
    public bool IsOpenOverride { get; set; }
    public bool IsClosedOverride { get; set; }

    public string? EventData 
    {
        get => JsonSerializer.Serialize(Events);
        set
        {
            Events.Clear();
            if(!String.IsNullOrWhiteSpace(value))
                Events.AddRange(JsonSerializer.Deserialize<CalendarEntry[]>(value) ?? Array.Empty<CalendarEntry>());
        }
    }

    [NotMapped]
    public List<CalendarEntry> Events { get; } = new();
}

public struct CalendarEntry
{
    public string Name { get; set; }
    public string Location { get; set; } 
    public DateTime StartTime { get; set; }
    public bool IsPistolEvent { get; set; }
}

