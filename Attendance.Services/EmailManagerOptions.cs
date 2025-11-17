using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Services;

public struct SmtpSettings
{
    public string? Server { get; set; }
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

}

public class EmailManagerOptions
{
    public const string SectionName = "EmailService";

    public SmtpSettings Smtp { get; set; }

    public string? TemplateId { get; set; }
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
}
