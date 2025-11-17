using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.Extensions.Options;

namespace Attendance.Services;

public interface IEmailManager
{
    Task SendAccessCodeAsync(string toEmail, string accessCode);
}

public interface IEmailQueueService
{
    void QueueEmailAsync(SmtpSettings smtp, MailMessage message);
}


public class EmailManager : IEmailManager
{
    EmailManagerOptions _options;
    IEmailQueueService _queue;

    public EmailManager(IOptions<EmailManagerOptions> options, IEmailQueueService queue)
    {
        _options = options.Value;
        _queue = queue;
    }

    public async Task SendAccessCodeAsync(string toEmail, string accessCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(_options.Smtp.Server, "Smtp.Server");
        ArgumentException.ThrowIfNullOrEmpty(_options.Smtp.Username, "Smtp.Username");
        ArgumentException.ThrowIfNullOrEmpty(_options.Smtp.Password, "Smtp.Password");
        ArgumentException.ThrowIfNullOrEmpty(_options.ReplyToEmail, nameof(_options.ReplyToEmail));
        ArgumentException.ThrowIfNullOrEmpty(_options.ReplyToName, nameof(_options.ReplyToName));
        ArgumentException.ThrowIfNullOrEmpty(_options.TemplateId, nameof(_options.TemplateId));

        if (String.IsNullOrEmpty(accessCode) || accessCode.Length != 8)
            throw new ArgumentException("Access code must be 8 characters", nameof(accessCode));

        var code = $"{accessCode.Substring(0, 2)} {accessCode.Substring(2, 2)} {accessCode.Substring(4, 2)} {accessCode.Substring(6, 2)}";

        Dictionary<string, string> data = new() {
            { "REPLY_TO_EMAIL", _options.ReplyToEmail},
            { "ACCESS_CODE", code }
        };

        var template = new EmailTemplate(_options.TemplateId);
        var message = await template.CreateMailMessage(data);
        if (message != null)
        {
            message.Subject = "Your Access Code for RRGC";
            message.From = new MailAddress(_options.Smtp.Username, "RRGC No Reply");
            message.To.Add(new MailAddress(toEmail));
            message.ReplyToList.Add(new MailAddress(_options.ReplyToEmail, _options.ReplyToName));

            _queue.QueueEmailAsync(_options.Smtp, message);
        }
    }
}