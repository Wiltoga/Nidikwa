using Nidikwa.Models;

namespace Nidikwa.Service;

internal class AudioService(
    ILogger<AudioService> logger
) : IAudioService
{
    public Task<Device[]> GetAvailableDevices()
    {
        throw new NotImplementedException();
    }
}
