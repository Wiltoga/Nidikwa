using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nidikwa.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddHostedService<PipeManagerWorker>()
    .AddSingleton<IAudioService, AudioService>()
    .AddScoped<IController, Controller>()
    .AddSingleton(new JsonSerializerSettings
    {
        Converters = new JsonConverter[]
        {
            new StringEnumConverter(new CamelCaseNamingStrategy())
        }
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
host.Run();
