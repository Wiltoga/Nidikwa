using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

public interface IControllerEndpoints
{
    Task<Result<Device[]>> GetAvailableDevices();

    Task<Result> StartRecording(string[] deviceIds);

    Task<Result> StopRecording();

    Task<Result<Device>> FindDevice(string deviceId);

    Task<Result<RecordSessionFile>> AddToQueue();
}
