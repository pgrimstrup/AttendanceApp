using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Data
{
    public class AttendanceSummaryRecord
    {
        public string PNZNumber { get;set; } = string.Empty;
        public SectionTags Sections { get;set; }
        public int Count { get;set; }


        public int SortCode
        {
            get
            {
                var code = PNZNumber.TrimEnd("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray());
                if(int.TryParse(code, out var num))
                    return num;
                return 0;
            }
        }
    }
}
