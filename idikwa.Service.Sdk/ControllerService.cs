using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nidikwa.Models;
using Nidikwa.Service.Utilities;
using System.IO.Pipes;
using System.Text;

namespace idikwa.Service.Sdk;

public class ControllerService : IControllerEndpoints
{
    private const string pipeName = "Nidikwa.Service.Pipe";
    private readonly static TimeSpan timeout = TimeSpan.FromSeconds(5);
    private readonly static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
    {
        Converters = new JsonConverter[]
        {
            new StringEnumConverter(new CamelCaseNamingStrategy())
        }
    };
    private NamedPipeClientStream pipeClientStream;
    public ControllerService()
    {
        pipeClientStream = new NamedPipeClientStream(pipeName);
    }

    public async Task<Result> ConnectAsync()
    {
        try
        {
            await pipeClientStream.ConnectAsync((int)timeout.TotalMilliseconds);
            return new Result { Code = ResultCodes.Success };
        }
        catch (TimeoutException) 
        {
            return new Result { Code = ResultCodes.Timeout };
        }
    }

    private async Task<Result> GetAsync(string input, string? data = null)
    {
        if (data is not null)
            input += $":{data}";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await pipeClientStream.WriteAsync(BitConverter.GetBytes(bytes.Length));
            await pipeClientStream.WriteAsync(bytes);

            var responseLengthBytes = new byte[sizeof(int)];
            await pipeClientStream.ReadAsync(responseLengthBytes);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await pipeClientStream.ReadAsync(responseBytes);
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

    private async Task<Result<T>> GetAsync<T>(string input, string? data = null)
    {
        if (data is not null)
            input += $":{data}";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await pipeClientStream.WriteAsync(BitConverter.GetBytes(bytes.Length));
            await pipeClientStream.WriteAsync(bytes);

            var responseLengthBytes = new byte[sizeof(int)];
            await pipeClientStream.ReadAsync(responseLengthBytes);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await pipeClientStream.ReadAsync(responseBytes);
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

    public Task<Result<Device[]>> GetAvailableDevices()
    {
        return GetAsync<Device[]>(RouteEndpoints.GetDevices);
    }

    public Task<Result> StartRecording(string deviceId)
    {
        return GetAsync(RouteEndpoints.GetDevices, deviceId);
    }

    public Task<Result<Device>> FindDevice(string deviceId)
    {
        return GetAsync<Device>(RouteEndpoints.FindDevice, deviceId);
    }

    public Task<Result> StopRecording()
    {
        return GetAsync(RouteEndpoints.StopRecording);
    }
}
