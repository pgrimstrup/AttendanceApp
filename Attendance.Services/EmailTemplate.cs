using System.Net.Mail;
using System.Text;

namespace Attendance.Services;

internal class EmailTemplate
{
    public string TemplateId { get; }

    public EmailTemplate(string templateId)
    {
        TemplateId = templateId;
    }

    public async Task<MailMessage?> CreateMailMessage(Dictionary<string, string> data)
    {
        var message = new MailMessage();

        using var textStream = GetType().Assembly.GetManifestResourceStream($"Attendance.Services.Templates.Text.{TemplateId}.txt");
        if (textStream != null)
        {
            using var reader = new StreamReader(textStream);
            string? text = await reader.ReadToEndAsync();
            if (text != null)
            {
                foreach (var key in data)
                {
                    text = text.Replace($"{{{{{key.Key}}}}}", key.Value);
                }

                // Default text body - most email clients will use the Alternate views if available
                message.Body = text;
                message.IsBodyHtml = false;
                message.BodyEncoding = Encoding.UTF8;

                var base64 = Convert.ToBase64String( Encoding.UTF8.GetBytes(text));
                var view = AlternateView.CreateAlternateViewFromString(text, Encoding.UTF8, "text/plain");
                view.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                message.AlternateViews.Add(view);
            }
        }

        using var htmlStream = GetType().Assembly.GetManifestResourceStream($"Attendance.Services.Templates.Html.{TemplateId}.html");
        if (htmlStream != null)
        {
            using var reader = new StreamReader(htmlStream);
            string? html = await reader.ReadToEndAsync();
            if (html != null)
            {
                foreach (var key in data)
                {
                    html = html.Replace($"{{{{{key.Key}}}}}", key.Value);
                }

                // Only use HTML as main body if no text body was set
                if (String.IsNullOrWhiteSpace(message.Body))
                {
                    message.Body = html;
                    message.IsBodyHtml = true;
                    message.BodyEncoding = Encoding.UTF8;
                }

                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
                var view = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, "text/html");
                view.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                message.AlternateViews.Add(view);
            }
        }

        if (message.AlternateViews.Count == 0)
            return null;

        return message;
    }
}
