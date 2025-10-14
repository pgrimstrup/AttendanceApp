using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Data;

public class AwayEvent
{
    public int PersonId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int AttendanceCount { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
