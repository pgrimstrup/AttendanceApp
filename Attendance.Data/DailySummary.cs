namespace Attendance.Data
{
    public class DailySummary
    {
        public int PersonId { get; set; }
        public DateOnly Date { get; set; }

        public DateTime? EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public DateTime? ReigstrationTime { get; set; }
        public string? RegistrationRange { get; set; }
        public bool IsMatchDay { get; set; }
        public bool IsAttendanceCounted { get; set; }
    }
}
