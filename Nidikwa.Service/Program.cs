using Nidikwa.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddHostedService<PipeManagerWorker>()
    .AddSingleton<IAudioService, AudioService>()
    .AddSingleton<IController, Controller>()
;

var host = builder.Build();
host.Run();
