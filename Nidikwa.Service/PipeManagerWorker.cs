using System.IO.Pipes;
using System.Text;

namespace Nidikwa.Service;

internal class PipeManagerWorker(
    ILogger<PipeManagerWorker> logger,
    IConfiguration configuration,
    IServiceScopeFactory serviceScopeFactory
) : BackgroundService
{
    private async Task HandleClient(NamedPipeServerStream serverStream, int clientNumber, CancellationToken stoppingToken)
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {

            try
            {
                var handledCountBytes = new byte[sizeof(int)];
                await serverStream.ReadAsync(handledCountBytes, stoppingToken);
                var handledCount = BitConverter.ToInt32(handledCountBytes);
                var handledVersions = new List<ushort>();
                for (int i = 0;i<handledCount;++i)
                {
                    var versionBytes = new byte[sizeof(ushort)];
                    await serverStream.ReadAsync(versionBytes, stoppingToken);
                    handledVersions.Add(BitConverter.ToUInt16(versionBytes));
                }
                logger.LogInformation("Client versions : {versions}", string.Join(", ", handledVersions.Select(version => version.ToString())));
                logger.LogInformation("Server versions : {versions}", string.Join(", ", ControllerVersions.Versions.Select(version => version.ToString())));
                var compatibleVersions = handledVersions.Intersect(ControllerVersions.Versions);
                logger.LogInformation("Compatible versions : {versions}", string.Join(", ", compatibleVersions.Select(version => version.ToString())));
                var usedVersion = compatibleVersions.Max();
                var controller = scope.ServiceProvider.GetRequiredKeyedService<IController>(usedVersion);
                await serverStream.WriteAsync(BitConverter.GetBytes(usedVersion), stoppingToken);
                logger.LogInformation("Using controller version {version}", usedVersion);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var dataLengthBytes = new byte[4];

                    var recieved = await serverStream.ReadAsync(dataLengthBytes, stoppingToken);
                    if (recieved == 0)
                    {
                        break;
                    }
                    stoppingToken.ThrowIfCancellationRequested();

                    var data = new byte[BitConverter.ToInt32(dataLengthBytes)];
                    recieved = await serverStream.ReadAsync(data, stoppingToken);
                    if (recieved == 0)
                    {
                        break;
                    }
                    stoppingToken.ThrowIfCancellationRequested();
                    string result;
                    result = await controller.HandleRequestAsync(Encoding.UTF8.GetString(data));
                    var resultBytes = Encoding.UTF8.GetBytes(result);

                    await serverStream.WriteAsync(BitConverter.GetBytes(resultBytes.Length), stoppingToken);
                    stoppingToken.ThrowIfCancellationRequested();

                    await serverStream.WriteAsync(resultBytes, stoppingToken);
                    stoppingToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
        }
        serverStream.Close();
        logger.LogInformation("Client #{clientNumber} disconnected", clientNumber);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipeName = configuration.GetValue<string>("PipeName");
        if (pipeName is null)
            throw new ArgumentNullException("PipeName");
        int clientNumberCounter = 1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
                logger.LogInformation("New named pipe '{pipeName}' opened", pipeName);
                await serverStream.WaitForConnectionAsync(stoppingToken);
                logger.LogInformation("Client #{clientNumberCounter} connected", clientNumberCounter);

                _ = HandleClient(serverStream, clientNumberCounter, stoppingToken);
                ++clientNumberCounter;
            }
            catch (OperationCanceledException)
            { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
        }
    }
}