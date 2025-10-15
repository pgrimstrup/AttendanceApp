using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Data
{
    public class CalendarCategory
    {
        public string Name { get; set; } = "";
        public bool IsPistolEvent { get; set; }
        public bool IsClosedEvent { get; set; }
        public string Color { get; set; } = "LightGray";
        public DateOnly LastEvent { get; set; }
    }
}
