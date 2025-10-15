

using Attendance.Services;
using Microsoft.Extensions.Options;

namespace AttendanceBlazor.HostedServices;

public class CalendarImportService : BackgroundService
{
    readonly IServiceProvider _services;
    readonly ILogger _logger;
    readonly CalendarImporterOptions _options;


    public CalendarImportService(
        IServiceProvider services,
        ILogger<CalendarImportService> logger, 
        IOptions<CalendarImporterOptions> options)
    {
        _logger = logger;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.IntervalMinutes <= 0 ? 60 : _options.IntervalMinutes);
        _logger.LogInformation("Service started. Poll interval: {Interval} | SourceUrl: {SourceUrl}",
            interval, _options.SourceUrl);

        // Optional: do an immediate check on startup
        await SafeRunOnce(stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SafeRunOnce(stoppingToken);
        }
    }

    private async Task SafeRunOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<ICalendarImporter>();

            await importer.ImportCalendarAsync();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during upload cycle.");
        }
    }

}
