using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Nidikwa.Service;

var appStopper = new CancellationTokenSource();

var builder = Host.CreateApplicationBuilder(args);
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
builder.Services
    .AddWindowsService(options =>
    {
        options.ServiceName = "Nidikwa Service";
    })
    .Configure<NidikwaWorkerConfiguration>(config =>
    {
        config.AppStopper = appStopper;
    })
    .AddHostedService<NidikwaWorker>()
    .AddSingleton<IAudioService, AudioService>()
    .AddLogging(logBuilder =>
    {
        logBuilder.AddSimpleConsole(c =>
        {
            c.TimestampFormat = "HH:mm:ss - ";
        });
    })
;

var host = builder.Build();
await host.RunAsync(appStopper.Token);
