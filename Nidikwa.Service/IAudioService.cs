using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal interface IAudioService
{
    Task<Device[]> GetAvailableDevicesAsync();

    Task StartRecordAsync(string id);

    Task<Device> GetDeviceAsync(string id);

    Task StopRecordAsync();

    Task<RecordSessionFile> AddToQueueAsync();
}
