using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nidikwa.Service;

internal class SocketManagerWorker(
    ILogger<SocketManagerWorker> logger,
    IConfiguration configuration,
    IServiceScopeFactory serviceScopeFactory
) : BackgroundService
{
    private async Task HandleClient(Socket socket, int clientNumber, CancellationToken stoppingToken)
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            try
            {
                var handledCountBytes = new byte[sizeof(int)];
                await socket.ReceiveAsync(handledCountBytes, stoppingToken);
                var handledCount = BitConverter.ToInt32(handledCountBytes);
                var handledVersions = new List<ushort>();
                for (int i = 0;i<handledCount;++i)
                {
                    var versionBytes = new byte[sizeof(ushort)];
                    await socket.ReceiveAsync(versionBytes, stoppingToken);
                    handledVersions.Add(BitConverter.ToUInt16(versionBytes));
                }
                logger.LogInformation("Client versions : {versions}", string.Join(", ", handledVersions.Select(version => version.ToString())));
                logger.LogInformation("Server versions : {versions}", string.Join(", ", ControllerVersions.Versions.Select(version => version.ToString())));
                var compatibleVersions = handledVersions.Intersect(ControllerVersions.Versions);
                logger.LogInformation("Compatible versions : {versions}", string.Join(", ", compatibleVersions.Select(version => version.ToString())));
                var usedVersion = compatibleVersions.Max();
                var controller = scope.ServiceProvider.GetRequiredKeyedService<IController>(usedVersion);
                await socket.SendAsync(BitConverter.GetBytes(usedVersion), stoppingToken);
                logger.LogInformation("Using controller version {version}", usedVersion);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var dataLengthBytes = new byte[4];

                    var recieved = await socket.ReceiveAsync(dataLengthBytes, stoppingToken);
                    if (recieved == 0)
                    {
                        break;
                    }
                    stoppingToken.ThrowIfCancellationRequested();

                    var data = new byte[BitConverter.ToInt32(dataLengthBytes)];
                    recieved = await socket.ReceiveAsync(data, stoppingToken);
                    if (recieved == 0)
                    {
                        break;
                    }
                    stoppingToken.ThrowIfCancellationRequested();
                    await controller.HandleRequestAsync(Encoding.UTF8.GetString(data), new NetworkStream(socket, false));
                    stoppingToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);
            }
        }
        socket.Close();
        logger.LogInformation("Client #{clientNumber} disconnected", clientNumber);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mutex = new Mutex(true, "Nidikwa.Service.Mutex", out var created);
        if (!created)
        {
            Environment.Exit(0);
            return;
        }
        try
        {
            var port = configuration.GetValue<int>("port");
            if (port <= 1024)
                throw new ArgumentException("port");
            var ipHostInfo = await Dns.GetHostEntryAsync("localhost", stoppingToken);
            var ipAddress = ipHostInfo.AddressList[0];
            var ipEndPoint = new IPEndPoint(ipAddress, port);
            int clientNumberCounter = 1;

            var listener = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ipEndPoint);
            listener.Listen(100);
            logger.LogInformation("listening on port {port}", ipEndPoint.Port);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var handler = await listener.AcceptAsync(stoppingToken);
                    logger.LogInformation("Client #{clientNumberCounter} connected", clientNumberCounter);

                    _ = HandleClient(handler, clientNumberCounter, stoppingToken);
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
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}