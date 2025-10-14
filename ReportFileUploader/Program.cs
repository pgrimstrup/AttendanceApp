using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

var runAsConsole = args.Any(a =>
    string.Equals(a, "-console", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(a, "/console", StringComparison.OrdinalIgnoreCase));

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, config) => {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddEnvironmentVariables(prefix: "FILEUP_");
    })
    .ConfigureServices((ctx, services) => {
        services.Configure<UploadOptions>(ctx.Configuration.GetSection("Upload"));

        services.AddHttpClient<FileUploader>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(10));

        services.AddSingleton<FileUploader>();
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging((ctx, logging) => {
        logging.ClearProviders();

        // Always show console logs when running interactively
        logging.AddSimpleConsole(o => {
            o.SingleLine = true;
            o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });

        // Only add Event Log sink when installed as a Windows Service
        if (!runAsConsole && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            logging.AddEventLog();
        }
    });

// Choose lifetime based on mode
if (!runAsConsole && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.UseWindowsService(options => {
        options.ServiceName = "File Uploader Service";
    });
}
else
{
    // Allow Ctrl+C / SIGTERM to stop gracefully in console mode
    builder.UseConsoleLifetime();
}

builder.Build().Run();
