using NAudio.CoreAudioApi;
using NAudio.Wave;
using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal class DeviceRecording
{
    public DeviceRecording(
        MMDevice mmDevice,
        WasapiCapture capture,
        CacheStream cache,
        BufferedStream buffer
    )
    {
        MmDevice = mmDevice;
        Capture = capture;
        Cache = cache;
        Buffer = buffer;
    }

    public MMDevice MmDevice { get; }
    public WasapiCapture Capture { get; }
    public CacheStream Cache { get; }
    public BufferedStream Buffer { get; }
}

internal class AudioService : IAudioService
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly ILogger<AudioService> logger;
    private readonly IConfiguration configuration;
    private MMDeviceEnumerator MMDeviceEnumerator;

    private DeviceRecording? Recording { get; set; }

    public AudioService(
       ILogger<AudioService> logger,
       IConfiguration configuration
    )
    {
        MMDeviceEnumerator = new MMDeviceEnumerator();
        this.logger = logger;
        this.configuration = configuration;
    }

    public Task<Device[]> GetAvailableDevicesAsync()
    {
        logger.LogInformation("List audio devices");
        return Locked(() =>
        {
            return Task.FromResult(MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device =>
            {
                return new Device(device.ID, device.FriendlyName, device.DataFlow == DataFlow.Capture ? DeviceType.Input : DeviceType.Output);
            }).ToArray());
        });
    }

    public Task<Device> GetDeviceAsync(string id)
    {
        logger.LogInformation("Get single audio device");
        return Locked(() =>
        {
            var mmDevice = MMDeviceEnumerator
                .GetDevice(id);
            if (mmDevice is null)
                throw new KeyNotFoundException("Uknown device");

            return Task.FromResult(new Device(mmDevice.ID, mmDevice.FriendlyName, mmDevice.DataFlow == DataFlow.Capture ? DeviceType.Input : DeviceType.Output));
        });
    }

    public Task StopRecordAsync()
    {
        logger.LogInformation("Stop recording");
        return Locked(async () =>
        {
            if (Recording is null)
                throw new InvalidOperationException("The service is not recording");
            var taskSource = new TaskCompletionSource();
            Recording.Capture.RecordingStopped += (sender, e) =>
            {
                taskSource.SetResult();
            };
            Recording.Capture.StopRecording();

            await taskSource.Task;
            Recording = null;
        });
    }

    public Task StartRecordAsync(string id)
    {
        logger.LogInformation("Start recording");
        return Locked(() =>
        {
            var mmDevice = MMDeviceEnumerator
                .GetDevice(id);
            if (mmDevice is null)
                throw new KeyNotFoundException("Uknown device");

            logger.LogInformation("Start recording using {deviceName}", mmDevice.FriendlyName);
            WasapiCapture capture;
            if (mmDevice.DataFlow == DataFlow.Render)
            {
                capture = new WasapiLoopbackCapture(mmDevice);
            }
            else
            {
                capture = new WasapiCapture(mmDevice);
            }

            var cache = new CacheStream(new MemoryStream(), capture.WaveFormat.AverageBytesPerSecond * 5);
            var buffer = new BufferedStream(cache, 1 << 20);
            capture.DataAvailable += (sender, e) =>
            {
                buffer.Write(e.Buffer, 0, e.BytesRecorded);
            };
            capture.StartRecording();
            Recording = new DeviceRecording(mmDevice, capture, cache, buffer);

            return Task.CompletedTask;
        });
    }

    private async Task Locked(Action action)
    {
        try
        {
            await _lock.WaitAsync();
            action();
        }
        finally
        {
            _lock.Release();
        }
    }
    private async Task Locked(Func<Task> action)
    {
        try
        {
            await _lock.WaitAsync();
            await action();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<T> Locked<T>(Func<Task<T>> action)
    {
        try
        {
            await _lock.WaitAsync();
            return await action();
        }
        finally
        {
            _lock.Release();
        }
    }
}
