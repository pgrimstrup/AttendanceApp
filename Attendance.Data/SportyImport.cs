using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance.Data
{
    [Flags]
    public enum SectionTags
    {
        None,
        IPSC = 1,
        ISSF = 2,
        CAS = 4,
        MultiGun = 8,
        SpeedSteel = 16,
        Archery = 32,
        AirRifle = 64,
        BlackPowder = 128,
        ServiceRifle = 256,
        SmallboreRifle = 512,
        SportingRifle = 1024,
        SightingIn = 2048
    }

    public class SportyImport
    {
        public int PersonId { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CardNumber { get; set; }
        public string? FALNumber { get; set; }
        public string? PNZNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? EmailAddress { get; set; }
        public SectionTags Sections { get; set; }

        // Used to 2FA
        public int? AuthCode { get; set; }
        public DateTime? AuthCodeExpiry { get; set; }

        [NotMapped]
        public string LastInitial => String.IsNullOrWhiteSpace(LastName) || LastName.Length < 1 ? "" : LastName.Substring(0, 1);
    }
}
