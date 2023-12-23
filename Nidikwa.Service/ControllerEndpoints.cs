using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal partial class Controller
{
    [Endpoint(RouteEndpoints.GetDevices)]
    public async Task<Result> GetAvailableDevices()
    {
        return Success(await audioService.GetAvailableDevicesAsync());
    }

    [Endpoint(RouteEndpoints.StartRecording)]
    public async Task<Result> StartRecording(string deviceId)
    {
        await audioService.StartRecordAsync(deviceId);
        return Success();
    }

    [Endpoint(RouteEndpoints.FindDevice)]
    public async Task<Result> FindDevice(string deviceId)
    {
        return Success(await audioService.GetDeviceAsync(deviceId));
    }
}
