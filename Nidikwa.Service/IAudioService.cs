using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal interface IAudioService
{
    event EventHandler QueueChanged;
    event EventHandler StatusChanged;
    event EventHandler DevicesChanged;

    Task DeleteQueueItemsAsync(Guid[] ids);

    Task<bool> IsRecordingAsync();

    Task<Device[]> GetAvailableDevicesAsync();

    Task StartRecordAsync(string[] ids);

    Task<Device> GetDeviceAsync(string id);

    Task StopRecordAsync();

    Task<RecordSessionFile> AddToQueueAsync();
}
