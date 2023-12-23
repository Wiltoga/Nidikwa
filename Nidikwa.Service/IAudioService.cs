using Nidikwa.Models;

namespace Nidikwa.Service;

internal interface IAudioService
{
    Task<Device[]> GetAvailableDevicesAsync();

   Task StartRecordAsync(string id);

    Task<Device> GetDeviceAsync(string id);
}
