using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Data;

public class AttendanceOptions
{
    public const string SectionName = "Attendance";

    public string[] Administrators { get; set; } = Array.Empty<string>();
}
