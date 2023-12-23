using System.IO.Pipes;

namespace Nidikwa.Service;

internal class PipeManagerWorker(
    ILogger<PipeManagerWorker> logger,
    IConfiguration configuration
) : BackgroundService
{
    private async Task HandleClient(NamedPipeServerStream serverStream, int clientNumber, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var buffer = new byte[1];
                var recieved = await serverStream.ReadAsync(buffer, stoppingToken);
                if (recieved == 0)
                {
                    break;
                }
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