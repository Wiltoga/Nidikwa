using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nidikwa.Service;


var builder = Host.CreateApplicationBuilder(args);
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
builder.Services
    .AddWindowsService(options =>
    {
        options.ServiceName = "Nidikwa Service";
    })
    .Configure<SocketManagerConfig>(builder.Configuration.GetRequiredSection(nameof(SocketManagerConfig)))
    .AddHostedService<SocketManagerWorker>()
    .AddSingleton<IAudioService, AudioService>()
    .AddControllers()
    .AddSingleton(new JsonSerializerSettings
    {
        Converters =
        [
            new StringEnumConverter(new CamelCaseNamingStrategy())
        ]
    })
    .AddLogging(logBuilder =>
    {
        logBuilder.AddSimpleConsole(c =>
        {
            c.TimestampFormat = "HH:mm:ss - ";
        });
    })
;

var host = builder.Build();
await host.RunAsync();
