namespace Attendance.Data
{
    public class YearlySummary
    {
        public int PersonId { get; set; }
        public int Year { get; set; }
        public int TotalDaysPresent { get; set; }
        public int TotalMatchDays { get; set; }
    }
}
