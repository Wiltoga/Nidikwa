namespace Nidikwa.Service;

internal partial class Controller
{
    [Endpoint("get-devices")]
    public async Task<Result> GetAvailableDevices()
    {
        return Success(await audioService.GetAvailableDevices());
    }
}
