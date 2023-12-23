using Nidikwa.Models;

namespace Nidikwa.Service.Utilities;

public interface IControllerEndpoints
{
    Task<Result<Device[]>> GetAvailableDevices();

    Task<Result> StartRecording(string deviceId);

    Task<Result> StopRecording();

    Task<Result<Device>> FindDevice(string deviceId);
}
