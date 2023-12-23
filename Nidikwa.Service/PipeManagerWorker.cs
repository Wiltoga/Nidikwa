using Newtonsoft.Json;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
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
                Result result;
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var controller = scope.ServiceProvider.GetRequiredService<IController>();
                    result = await controller.ParseInputAsync(Encoding.UTF8.GetString(data));
                }
                var resultString = JsonConvert.SerializeObject(result);
                var resultBytes = Encoding.UTF8.GetBytes(resultString);

                await serverStream.WriteAsync(BitConverter.GetBytes(resultBytes.Length), stoppingToken);
                stoppingToken.ThrowIfCancellationRequested();

                await serverStream.WriteAsync(resultBytes, stoppingToken);
                stoppingToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
                break;
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