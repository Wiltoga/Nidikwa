using Nidikwa.Common;

namespace Nidikwa.Sdk
{
    public interface IControllerService : IDisposable
    {
        Task<Result> StartRecordingAsync(RecordParams args, CancellationToken token = default);

        Task<Result> StopRecordingAsync(CancellationToken token = default);

        Task<Result<RecordSessionFile>> SaveAsync(CancellationToken token = default);

        Task<Result<RecordStatus>> GetStatusAsync(CancellationToken token = default);

        Task<Result> WaitStatusChangedAsync(CancellationToken token = default);

        Task<Result> WaitDevicesChangedAsync(CancellationToken token = default);
    }
}