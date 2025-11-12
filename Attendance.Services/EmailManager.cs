using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.Extensions.Options;
using Smtp2Go.Api;
using Smtp2Go.Api.Models.Emails;

namespace Attendance.Services;

public interface IEmailManager
{
    Task SendAccessCodeAsync(string toEmail, string accessCode);
}

public class EmailManagerOptions
{
    public const string SectionName = "EmailService";

    public string? ApiKey { get; set; }
    public string? TemplateId { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName {  get; set; }
}

public class EmailManager : IEmailManager
{
    EmailManagerOptions _options;
    Smtp2GoApiService _service;

    public EmailManager(IOptions<EmailManagerOptions> options)
    {
        _options = options.Value;

        ArgumentNullException.ThrowIfNull(_options.ApiKey, nameof(_options.ApiKey));
        ArgumentNullException.ThrowIfNull(_options.SenderEmail, nameof(_options.SenderEmail));

        _service = new Smtp2GoApiService(_options.ApiKey);
    }

    public async Task SendAccessCodeAsync(string toEmail, string accessCode)
    {
        if(String.IsNullOrEmpty(accessCode) || accessCode.Length != 8)
            throw new ArgumentException("Access code must be 8 characters", nameof(accessCode));

        var message = new TemplatedEmailMessage(_options.TemplateId, _options.SenderEmail, toEmail);

        var code = $"{accessCode.Substring(0,2)} {accessCode.Substring(2,2)} {accessCode.Substring(4,2)} {accessCode.Substring(6,2)}";
        message.AddTemplateVariable("access_code", code);
        message.AddCustomHeader("Reply-To", $"{_options.SenderName} <{_options.SenderEmail}>");

        var response = await _service.SendTemplatedEmail(message);

    }
}