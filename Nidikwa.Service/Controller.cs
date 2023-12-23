using Nidikwa.Models;

namespace Nidikwa.Service;

internal partial class Controller
{
    [Endpoint("get-devices")]
    public Task<Result> GetAvailableDevices()
    {
        return Task.FromResult(Success(Array.Empty<Device>()));
    }
}
