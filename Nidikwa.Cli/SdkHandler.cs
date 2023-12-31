using Nidikwa.Sdk;

namespace Nidikwa.Cli;

internal static class SdkHandler
{
    private static IControllerService? instance;
    private const string defaultHost = "localhost";
    private const int defaultPort = 17854;

    public static async Task<IControllerService> GetInstanceAsync(string? host = null, int? port = null) => instance ?? await ControllerService.ConnectAsync(host ?? defaultHost, port ?? defaultPort);
}
