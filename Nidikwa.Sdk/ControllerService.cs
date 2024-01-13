using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Nidikwa.Sdk;

public static class ControllerService
{
    private static Dictionary<ushort, Func<Socket, IControllerService>> _controllerServiceConstructors;

    static ControllerService()
    {
        _controllerServiceConstructors = new();
        foreach (var type in from type in typeof(ControllerService).Assembly.GetTypes()
                             where type.GetInterfaces().Contains(typeof(IControllerService))
                             select type)
        {
            var version = type.GetCustomAttribute<ControllerServiceVersionAttribute>()?.Version;
            if (version is null)
                continue;

            var constructor = type.GetConstructor(new[] { typeof(Socket) });

            if (constructor is null)
                continue;

            _controllerServiceConstructors.Add(version.Value, (Socket client) => (IControllerService)constructor.Invoke(new[] { client }));
        }
    }

    public static async Task<IControllerService> ConnectAsync(string host, int port, CancellationToken token = default)
    {
        var ipHostInfo = await Dns.GetHostEntryAsync(host).ConfigureAwait(false);
        var ipAddress = ipHostInfo.AddressList[0];
        var endpoint = new IPEndPoint(ipAddress, port);
        var client = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        var connectionToken = new CancellationTokenSource();
        try
        {
            await client.ConnectAsync(endpoint, connectionToken.Token).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            throw new TimeoutException();
        }

        await client.SendAsync(BitConverter.GetBytes(_controllerServiceConstructors.Count), SocketFlags.None, token).ConfigureAwait(false);
        foreach (var service in _controllerServiceConstructors)
        {
            await client.SendAsync(BitConverter.GetBytes(service.Key), SocketFlags.None, token).ConfigureAwait(false);
        }
        var versionBytes = new byte[sizeof(ushort)];
        await client.ReceiveAsync(versionBytes, SocketFlags.None, token).ConfigureAwait(false);
        var version = BitConverter.ToUInt16(versionBytes, 0);

        if (!_controllerServiceConstructors.TryGetValue(version, out var controllerConstructor))
        {
            throw new InvalidOperationException("No suitable controller found");
        }

        return controllerConstructor.Invoke(client);
    }
}
