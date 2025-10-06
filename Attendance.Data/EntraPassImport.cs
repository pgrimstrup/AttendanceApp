namespace Attendance.Data
{
    public class EntraPassImport
    {
        public DateTime EventTime { get; set; }
        public string CardNumber { get; set; } = "";
        public string? CardUserName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? EventMessage { get; set; }
        public string? Location { get; set; }
        public int PersonId { get; set; } // extracted from CardUserName
        public SectionTags Sections { get; set; } // extracted from CardUserName
        public string? CardInfo1 { get; set; }
        public string? CardInfo2 { get; set; }
    }
}
