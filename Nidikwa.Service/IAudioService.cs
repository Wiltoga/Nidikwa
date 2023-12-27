using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal interface IAudioService
{
    Task<bool> IsRecording();

    Task<Device[]> GetAvailableDevicesAsync();

    Task StartRecordAsync(string[] ids);

    Task<Device> GetDeviceAsync(string id);

    Task StopRecordAsync();

    Task<RecordSessionFile> AddToQueueAsync();
}
