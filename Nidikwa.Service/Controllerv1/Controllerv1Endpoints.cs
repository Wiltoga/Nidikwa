using Nidikwa.Service.Utilities;

namespace Nidikwa.Service.Controllerv1;

[ControllerVersion(1)]
internal partial class Controller
{
    [Endpoint(RouteEndpoints.DeleteFromQueue)]
    public async Task<Result> DeleteQueueItem(Guid[] ids)
    {
        await audioService.DeleteQueueItemsAsync(ids);
        return Success();
    }

    [Endpoint(RouteEndpoints.StartRecording)]
    public async Task<Result> StartRecording(RecordParams args)
    {
        await audioService.StartRecordAsync(args);
        return Success();
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
        return Success((await audioService.IsRecordingAsync()) ? RecordStatus.Recording : RecordStatus.Stopped);
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
