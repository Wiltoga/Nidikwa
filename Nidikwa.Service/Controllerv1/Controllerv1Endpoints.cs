using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service.Controllerv1;

[ControllerVersion(1)]
internal partial class Controller
{
    [Endpoint(RouteEndpoints.DeleteFromQueue)]
    public async Task<Result> DeleteQueueItem()
    {
        return Success(await audioService.GetAvailableDevicesAsync());
    }

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

    [Endpoint(RouteEndpoints.GetStatus)]
    public async Task<Result<RecordStatus>> GetStatus()
    {
        return Success((await audioService.IsRecording()) ? RecordStatus.Recording : RecordStatus.Stopped);
    }

    [Endpoint(RouteEndpoints.EventQueueChanged)]
    public async Task<Result> EventQueueChanged()
    {
        var taskSource = new TaskCompletionSource();
        void callback(object? sender, EventArgs e)
        {
            taskSource.SetResult();
        }
        audioService.QueueChanged += callback;
        await taskSource.Task;
        audioService.QueueChanged -= callback;
        return Success();
    }

    [Endpoint(RouteEndpoints.EventStatusChanged)]
    public async Task<Result> EventStatusChanged()
    {
        var taskSource = new TaskCompletionSource();
        void callback(object? sender, EventArgs e)
        {
            taskSource.SetResult();
        }
        audioService.StatusChanged += callback;
        await taskSource.Task;
        audioService.StatusChanged -= callback;
        return Success();
    }
}
