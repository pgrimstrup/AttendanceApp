namespace Attendance.Services;

public class CalendarImporterOptions
{
    public const string SectionName = "CalendarImporter";

    public string SourceUrl { get; set; } = "";
    public int IntervalMinutes { get; set; } = 60;
}
