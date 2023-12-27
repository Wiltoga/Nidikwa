using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nidikwa.Models;
using Nidikwa.Service.Utilities;
using System.IO.Pipes;
using System.Text;

namespace Nidikwa.Service.Sdk;

[ControllerServiceVersion(1)]
internal class ControllerServicev1 : IControllerService
{
    private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
    {
        Converters = new JsonConverter[]
        {
            new StringEnumConverter(new CamelCaseNamingStrategy())
        }
    };

    private NamedPipeClientStream pipeClientStream;

    public ControllerServicev1(NamedPipeClientStream client)
    {
        pipeClientStream = client;
    }

    private async Task<Result> GetAsync(string input, CancellationToken token, object? data = null)
    {
        if (data is not null)
            input += $":{JsonConvert.SerializeObject(data, serializerSettings)}";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await pipeClientStream.WriteAsync(BitConverter.GetBytes(bytes.Length), token);
            await pipeClientStream.WriteAsync(bytes, token);

            var responseLengthBytes = new byte[sizeof(int)];
            await pipeClientStream.ReadAsync(responseLengthBytes, token);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await pipeClientStream.ReadAsync(responseBytes, token);
            var result = JsonConvert.DeserializeObject<Result>(Encoding.UTF8.GetString(responseBytes), serializerSettings);
            return result ?? new Result { Code = ResultCodes.NoResponse };
        }
        catch (TimeoutException)
        {
            return new Result { Code = ResultCodes.Timeout };
        }
        catch (IOException)
        {
            return new Result { Code = ResultCodes.Disconnected };
        }
    }

    private async Task<Result<T>> GetAsync<T>(string input, CancellationToken token, object? data = null)
    {
        if (data is not null)
            input += $":{JsonConvert.SerializeObject(data, serializerSettings)}";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await pipeClientStream.WriteAsync(BitConverter.GetBytes(bytes.Length), token);
            await pipeClientStream.WriteAsync(bytes, token);

            var responseLengthBytes = new byte[sizeof(int)];
            await pipeClientStream.ReadAsync(responseLengthBytes, token);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await pipeClientStream.ReadAsync(responseBytes, token);
            var result = JsonConvert.DeserializeObject<Result<T>>(Encoding.UTF8.GetString(responseBytes), serializerSettings);
            return result ?? new Result<T> { Code = ResultCodes.NoResponse };
        }
        catch (TimeoutException)
        {
            return new Result<T> { Code = ResultCodes.Timeout };
        }
        catch (IOException)
        {
            return new Result<T> { Code = ResultCodes.Disconnected };
        }
    }

    public Task<Result<Device[]>> GetAvailableDevicesAsync(CancellationToken token)
    {
        return GetAsync<Device[]>(RouteEndpoints.GetDevices, token);
    }

    public Task<Result> StartRecordingAsync(string[] deviceIds, CancellationToken token)
    {
        return GetAsync(RouteEndpoints.StartRecording, token, deviceIds);
    }

    public Task<Result<Device>> FindDeviceAsync(string deviceId, CancellationToken token)
    {
        return GetAsync<Device>(RouteEndpoints.FindDevice, token, deviceId);
    }

    public Task<Result> StopRecordingAsync(CancellationToken token)
    {
        return GetAsync(RouteEndpoints.StopRecording, token);
    }

    public Task<Result> AddToQueueAsync(CancellationToken token = default)
    {
        return GetAsync(RouteEndpoints.AddToQueue, token);
    }

    public Task<Result<RecordStatus>> GetStatusAsync(CancellationToken token = default)
    {
        return GetAsync<RecordStatus>(RouteEndpoints.GetStatus, token);
    }

    public Task<Result> DeleteQueueItemAsync(Guid[] itemIds, CancellationToken token = default)
    {
        return GetAsync(RouteEndpoints.DeleteFromQueue, token, itemIds);
    }

    public Task<Result> WaitQueueChangedAsync(CancellationToken token = default)
    {
        return GetAsync(RouteEndpoints.EventQueueChanged, token);
    }
}
