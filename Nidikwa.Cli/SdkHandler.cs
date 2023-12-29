using Nidikwa.Sdk;

namespace Nidikwa.Cli;

internal static class SdkHandler
{
    private static IControllerService? instance;

    public static async Task<IControllerService> GetInstanceAsync() => instance ?? await ControllerService.ConnectAsync();
}
