using Nidikwa.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddHostedService<PipeManagerWorker>();
;

var host = builder.Build();
host.Run();