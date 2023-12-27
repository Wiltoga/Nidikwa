﻿using Nidikwa.Service.Utilities;

namespace Nidikwa.Service.Sdk
{
    public interface IControllerService
    {
        Task<Result> DeleteQueueItemAsync(Guid[] itemIds, CancellationToken token = default);

        Task<Result> StartRecordingAsync(RecordParams args, CancellationToken token = default);

        Task<Result> StopRecordingAsync(CancellationToken token = default);

        Task<Result> AddToQueueAsync(CancellationToken token = default);

        Task<Result<RecordStatus>> GetStatusAsync(CancellationToken token = default);

        Task<Result> WaitQueueChangedAsync(CancellationToken token = default);

        Task<Result> WaitStatusChangedAsync(CancellationToken token = default);

        Task<Result> WaitDevicesChangedAsync(CancellationToken token = default);
    }
}