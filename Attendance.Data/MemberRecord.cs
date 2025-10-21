using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Data;

public class MemberRecord
{
    public int? PersonId { get; set; }
    public string? SportyCardNumber { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FALNumber { get; set; } 
    public string? PNZNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? EmailAddress { get; set; }
    public SectionTags Sections { get; set; }
    public int? EntraPersonId { get; set; }
    public string? EntraCardNumber { get; set; }
    public string? EntraCardUserName { get; set; }
    public string? EntraInfo1 { get; set; }
    public DateTime? EntraEndDate { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
