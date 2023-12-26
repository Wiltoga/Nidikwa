using System.IO.Pipes;
using System.Reflection;

namespace Nidikwa.Service.Sdk;

public static class ControllerService
{
    private readonly static TimeSpan timeout = TimeSpan.FromSeconds(5);
    private const string pipeName = "Nidikwa.Service.Pipe";
    private static Dictionary<ushort, Func<NamedPipeClientStream, IControllerService>> _controllerServiceConstructors;

    static ControllerService()
    {
        _controllerServiceConstructors = new();
        foreach(var type in from type in typeof(ControllerService).Assembly.GetTypes()
                               where type.GetInterfaces().Contains(typeof(IControllerService))
                               select type)
        {
            var version = type.GetCustomAttribute<ControllerServiceVersionAttribute>()?.Version;
            if (version is null)
                continue;

            var constructor = type.GetConstructor(new[] { typeof(NamedPipeClientStream) });

            if (constructor is null)
                continue;

            _controllerServiceConstructors.Add(version.Value, (NamedPipeClientStream client) => (IControllerService)constructor.Invoke(new[] { client }));
        }
    }

    public static async Task<IControllerService> ConnectAsync(CancellationToken token = default)
    {
        var pipeClientStream = new NamedPipeClientStream(pipeName);
        await pipeClientStream.ConnectAsync((int)timeout.TotalMilliseconds, token);
        var versionBytes = new byte[sizeof(ushort)];
        await pipeClientStream.ReadAsync(versionBytes, token);
        var version = BitConverter.ToUInt16(versionBytes, 0);

        if (!_controllerServiceConstructors.TryGetValue(version, out var controllerConstructor))
        {
            throw new InvalidOperationException("No suitable controller found");
        }

        return controllerConstructor.Invoke(pipeClientStream);
    }
}
