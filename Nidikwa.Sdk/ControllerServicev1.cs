using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nidikwa.Common;
using Nidikwa.FileEncoding;
using Nidikwa.Models;
using System.Net.Sockets;
using System.Text;

namespace Nidikwa.Sdk;

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

    private Socket client;

    public ControllerServicev1(Socket client)
    {
        this.client = client;
    }

    public void Dispose()
    {
        client.Close();
    }

    public Task<Result<Device[]>> GetRecordingDevicesAsync(CancellationToken token = default)
    {
        return GetAsync<Device[]>(RouteEndpoints.GetRecordingDevices, token);
    }

    public Task<Result<TimeSpan>> GetRecordingDurationAsync(CancellationToken token = default)
    {
        return GetAsync<TimeSpan>(RouteEndpoints.GetMaxDuration, token);
    }

    public Task<Result<RecordStatus>> GetStatusAsync(CancellationToken token = default)
    {
        return GetAsync<RecordStatus>(RouteEndpoints.GetStatus, token);
    }

    public async Task<Result<RecordSessionFile>> SaveAsync(CancellationToken token = default)
    {
        var resultSessionFile = await GetContentAsync(RouteEndpoints.SaveAsNdkw, token).ConfigureAwait(false);
        if (resultSessionFile.Code != ResultCodes.Success)
            return new Result<RecordSessionFile> { Code = resultSessionFile.Code };
        var decoder = new SessionEncoder();
        var savedMetadata = new StreamSaver(resultSessionFile.AdditionnalContent!);
        var metadata = await decoder.ParseMetadataAsync(savedMetadata, null, token).ConfigureAwait(false);
        using var stream = new FileStream(QueueAccessor.GenerateFileName(metadata), FileMode.Create, FileAccess.Write);
        await savedMetadata.GetData().CopyToAsync(stream, token).ConfigureAwait(false);
        await resultSessionFile.AdditionnalContent!.CopyToAsync(stream, token).ConfigureAwait(false);
        return new Result<RecordSessionFile> { Code = ResultCodes.Success, Data = new RecordSessionFile(metadata, stream.Name) };
    }

    public Task<Result> StartRecordingAsync(RecordParams args, CancellationToken token)
    {
        return GetAsync(RouteEndpoints.StartRecording, token, args);
    }

    public Task<Result> StopRecordingAsync(CancellationToken token)
    {
        return GetAsync(RouteEndpoints.StopRecording, token);
    }

    public Task<Result> WaitDevicesChangedAsync(CancellationToken token = default)
    {
        return GetAsync(RouteEndpoints.EventDevicesChanged, token);
    }

    public Task<Result> WaitStatusChangedAsync(CancellationToken token = default)
    {
        return GetAsync(RouteEndpoints.EventStatusChanged, token);
    }

    private async Task<Result> GetAsync(string input, CancellationToken token, object? data = null)
    {
        if (data is not null)
            input += $":{JsonConvert.SerializeObject(data, serializerSettings)}";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await client.SendAsync(BitConverter.GetBytes(bytes.Length), SocketFlags.None, token).ConfigureAwait(false);
            await client.SendAsync(bytes, SocketFlags.None, token).ConfigureAwait(false);

            var responseLengthBytes = new byte[sizeof(int)];
            await client.ReceiveAsync(responseLengthBytes, SocketFlags.None, token).ConfigureAwait(false);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await client.ReceiveAsync(responseBytes, SocketFlags.None, token).ConfigureAwait(false);
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
            await client.SendAsync(BitConverter.GetBytes(bytes.Length), SocketFlags.None, token).ConfigureAwait(false);
            await client.SendAsync(bytes, SocketFlags.None, token).ConfigureAwait(false);

            var responseLengthBytes = new byte[sizeof(int)];
            await client.ReceiveAsync(responseLengthBytes, SocketFlags.None, token).ConfigureAwait(false);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await client.ReceiveAsync(responseBytes, SocketFlags.None, token).ConfigureAwait(false);
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

    private async Task<ContentResult> GetContentAsync(string input, CancellationToken token, object? data = null)
    {
        if (data is not null)
            input += $":{JsonConvert.SerializeObject(data, serializerSettings)}";

        ContentResult? result;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await client.SendAsync(BitConverter.GetBytes(bytes.Length), SocketFlags.None, token).ConfigureAwait(false);
            await client.SendAsync(bytes, SocketFlags.None, token).ConfigureAwait(false);

            var responseLengthBytes = new byte[sizeof(int)];
            await client.ReceiveAsync(responseLengthBytes, SocketFlags.None, token).ConfigureAwait(false);
            var responseBytes = new byte[BitConverter.ToInt32(responseLengthBytes)];
            await client.ReceiveAsync(responseBytes, SocketFlags.None, token).ConfigureAwait(false);
            result = JsonConvert.DeserializeObject<ContentResult>(Encoding.UTF8.GetString(responseBytes), serializerSettings);
            var content = new NetworkStream(client);
            result ??= new ContentResult { Code = ResultCodes.NoResponse };
            result.AdditionnalContent = content;
        }
        catch (TimeoutException)
        {
            result = new ContentResult { Code = ResultCodes.Timeout };
        }
        catch (IOException)
        {
            result = new ContentResult { Code = ResultCodes.Disconnected };
        }
        return result;
    }

    private class StreamSaver : Stream
    {
        private MemoryStream ReadData;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Stream Source { get; private set; }

        public StreamSaver(Stream source)
        {
            Source = source;
            ReadData = new MemoryStream();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public MemoryStream GetData()
        {
            Source = null!;
            Close();
            ReadData.Seek(0, SeekOrigin.Begin);
            return ReadData;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = Source.Read(buffer, offset, count);
            ReadData.Write(buffer, offset, read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
