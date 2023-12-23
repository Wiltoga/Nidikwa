using Nidikwa.Models;

namespace Nidikwa.Service;

internal interface IAudioService
{
    Task<Device[]> GetAvailableDevices();
}
