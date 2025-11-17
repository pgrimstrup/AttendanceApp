
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Attendance.Services;

namespace AttendanceBlazor.HostedServices;

public class EmailQueueService : BackgroundService, IEmailQueueService
{
    ILogger _logger;
    ConcurrentQueue<(MailMessage message, SmtpSettings smtp)> _emailQueue = new();

    public EmailQueueService(ILogger<EmailQueueService> logger)
    {
        _logger = logger;
    }

    public void QueueEmailAsync(SmtpSettings smtp, MailMessage message)
    {
        _emailQueue.Enqueue((message, smtp));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessEmailQueue(stoppingToken);
    }

    private async Task ProcessEmailQueue(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_emailQueue.TryDequeue(out var data))
            {
                try
                {
                    using var smtpClient = new SmtpClient(data.smtp.Server, data.smtp.Port) {
                        EnableSsl = data.smtp.EnableSsl,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(data.smtp.Username, data.smtp.Password)
                    };

                    _logger.LogInformation("Sending email to {To} via {SmtpServer}:{SmtpPort}", String.Join(", ", data.message.To), data.smtp.Server, data.smtp.Port);
                    await smtpClient.SendMailAsync(data.message, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Log error (logging mechanism not shown here)
                    _logger.LogError(ex, $"Error sending email: {ex.Message}");
                }
                finally
                {
                    data.message.Dispose();
                }

            }
            else
                await Task.Delay(200, stoppingToken);
        }
    }
}
