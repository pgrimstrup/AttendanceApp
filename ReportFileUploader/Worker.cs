using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly FileUploader _uploader;
    private readonly UploadOptions _options;

    public Worker(ILogger<Worker> logger, FileUploader uploader, IOptionsMonitor<UploadOptions> options)
    {
        _logger = logger;
        _uploader = uploader;
        _options = options.CurrentValue;

        // react to live appsettings.json changes
        options.OnChange(o => {
            _logger.LogInformation("Upload options changed at runtime.");
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.IntervalMinutes <= 0 ? 60 : _options.IntervalMinutes);
        _logger.LogInformation("Service started. Poll interval: {Interval} | File: {File} | Endpoint: {Endpoint}",
            interval, _options.FilePath, _options.EndpointUrl);

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
            await _uploader.TryUploadAsync(ct);
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
