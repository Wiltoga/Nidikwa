using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal partial class Controller : IControllerEndpoints
{
    [Endpoint(RouteEndpoints.GetDevices)]
    public async Task<Result<Device[]>> GetAvailableDevices()
    {
        return Success(await audioService.GetAvailableDevicesAsync());
    }

    [Endpoint(RouteEndpoints.StartRecording)]
    public async Task<Result> StartRecording(string[] deviceIds)
    {
        await audioService.StartRecordAsync(deviceIds);
        return Success();
    }

    [Endpoint(RouteEndpoints.FindDevice)]
    public async Task<Result<Device>> FindDevice(string deviceId)
    {
        return Success(await audioService.GetDeviceAsync(deviceId));
    }

    [Endpoint(RouteEndpoints.StopRecording)]
    public async Task<Result> StopRecording()
    {
        await audioService.StopRecordAsync();
        return Success();
    }

    [Endpoint(RouteEndpoints.AddToQueue)]
    public async Task<Result<RecordSessionFile>> AddToQueue()
    {
        return Success(await audioService.AddToQueueAsync());
    }
}
