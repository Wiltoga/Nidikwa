using Nidikwa.Common;
using Nidikwa.Models;
using System.IO;
using System.Net.Sockets;

namespace Nidikwa.Service.Controllerv1;

[ControllerVersion(1)]
internal partial class Controller
{
    [Endpoint(RouteEndpoints.StartRecording)]
    public async Task<Result> StartRecording(RecordParams args)
    {
        await audioService.StartRecordAsync(args);
        return Success();
    }

    [Endpoint(RouteEndpoints.GetRecordingDevices)]
    public async Task<Result<Device[]>> GetRecordingDevices()
    {
        return Success(await audioService.GetRecordingDevicesAsync());
    }

    [Endpoint(RouteEndpoints.GetMaxDuration)]
    public async Task<Result<TimeSpan>> GetMaxDuration()
    {
        return Success(await audioService.GetCurrentMaxDurationAsync());
    }

    [Endpoint(RouteEndpoints.StopRecording)]
    public async Task<Result> StopRecording()
    {
        await audioService.StopRecordAsync();
        return Success();
    }

    [Endpoint(RouteEndpoints.SaveAsNdkw)]
    public async Task<ContentResult> SaveAsNdkw()
    {
        return Success(await audioService.SaveAsNdkwAsync());
    }

    [Endpoint(RouteEndpoints.GetStatus)]
    public async Task<Result<RecordStatus>> GetStatus()
    {
        return Success((await audioService.IsRecordingAsync()) ? RecordStatus.Recording : RecordStatus.Stopped);
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

    [Endpoint(RouteEndpoints.EventDevicesChanged)]
    public async Task<Result> EventDevicesChanged()
    {
        var taskSource = new TaskCompletionSource();
        void callback(object? sender, EventArgs e)
        {
            taskSource.SetResult();
        }
        audioService.DevicesChanged += callback;
        await taskSource.Task;
        audioService.DevicesChanged -= callback;
        return Success();
    }
}
