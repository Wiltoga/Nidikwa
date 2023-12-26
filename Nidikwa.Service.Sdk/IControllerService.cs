using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service.Sdk
{
    public interface IControllerService
    {
        Task<Result<Device>> FindDeviceAsync(string deviceId, CancellationToken token = default);
        Task<Result<Device[]>> GetAvailableDevicesAsync(CancellationToken token = default);
        Task<Result> StartRecordingAsync(string[] deviceIds, CancellationToken token = default);
        Task<Result> StopRecordingAsync(CancellationToken token = default);
        Task<Result> AddToQueueAsync(CancellationToken token = default);
    }
}