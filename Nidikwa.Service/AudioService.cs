using NAudio.CoreAudioApi;
using NAudio.Wave;
using Nidikwa.FileEncoding;
using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal class DeviceRecording
{
    public DeviceRecording(
        MMDevice mmDevice,
        WasapiCapture capture,
        CacheStream cache,
        BufferedStream buffer,
        IWavePlayer? silence
    )
    {
        MmDevice = mmDevice;
        Capture = capture;
        Cache = cache;
        Buffer = buffer;
        Silence = silence;
        Paused = false;
        Mutex = new();
    }
    public object Mutex { get; }
    public bool Paused { get; set;  }
    public MemoryStream? PauseCache { get; set; }
    public MMDevice MmDevice { get; }
    public WasapiCapture Capture { get; }
    public CacheStream Cache { get; }
    public BufferedStream Buffer { get; }
    public IWavePlayer? Silence { get; }
}

internal class AudioService : IAudioService
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly ILogger<AudioService> logger;
    private readonly IConfiguration configuration;
    private MMDeviceEnumerator MMDeviceEnumerator;
    private IWavePlayer player;

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
            Recording.Silence?.Stop();
            Recording.Buffer.Flush();
            Recording.Cache.Seek(0, SeekOrigin.Begin);
            Recording = null;
        });
    }

    public Task<RecordSessionFile> AddToQueueAsync()
    {
        logger.LogInformation("Add to queue");
        return Locked(async () =>
        {
            if (Recording is null)
                throw new InvalidOperationException("The service is not recording");

            using var waveBytes = new MemoryStream();

            lock (Recording.Mutex)
            {
                Recording.Paused = true;
            }
            Recording.Buffer.Flush();
            Recording.Cache.Seek(0, SeekOrigin.Begin);
            using var waveProvider = new RawSourceWaveStream(Recording.Cache, Recording.Capture.WaveFormat);
            await Task.Run(() => WaveFileWriter.WriteWavFileToStream(waveBytes, waveProvider));
            Recording.Cache.Seek(0, SeekOrigin.End);
            var duration = TimeSpan.FromSeconds(Recording.Cache.Length / (double)Recording.Capture.WaveFormat.AverageBytesPerSecond);
            lock (Recording.Mutex)
            {
                Recording.Paused = false;
            }

            var resultFile = new RecordSession(
                new RecordSessionMetadata(
                    Guid.NewGuid(),
                    DateTimeOffset.Now,
                    duration
                ),
                new[]
                {
                    new DeviceSession(
                        new Device(
                            Recording.MmDevice.ID,
                            Recording.MmDevice.FriendlyName,
                            Recording.MmDevice.DataFlow == DataFlow.Render ? DeviceType.Output : DeviceType.Input
                        ),
                        waveBytes.ToArray()
                    )
                }
            );

            var writer = new SessionEncoder();
            NidikwaFiles.EnsureQueueFolderExists();
            var file = Path.Combine(NidikwaFiles.QueueFolder, $"{resultFile.Metadata.Date.ToUnixTimeSeconds()}.ndkw");
            using var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);

            await writer.WriteSessionAsync(resultFile, fileStream);

            return new RecordSessionFile(resultFile.Metadata, file);
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
            WasapiOut? silence = null;
            if (mmDevice.DataFlow == DataFlow.Render)
            {
                capture = new WasapiLoopbackCapture(mmDevice);
                silence = new WasapiOut(mmDevice, AudioClientShareMode.Shared, true, 200);
                silence.Init(new SilenceProvider(mmDevice.AudioClient.MixFormat));
            }
            else
            {
                capture = new WasapiCapture(mmDevice);
            }

            var cache = new CacheStream(new MemoryStream(), capture.WaveFormat.AverageBytesPerSecond * 5);
            var buffer = new BufferedStream(cache, 1 << 20);
            Recording = new DeviceRecording(mmDevice, capture, cache, buffer, silence);
            capture.DataAvailable += (sender, e) =>
            {
                lock(Recording.Mutex)
                {
                    if (Recording.Paused)
                    {
                        if (Recording.PauseCache is null)
                            Recording.PauseCache = new MemoryStream();

                        Recording.PauseCache.Write(e.Buffer, 0, e.BytesRecorded);
                    }
                    else
                    {
                        if (Recording.PauseCache is not null)
                        {
                            Recording.PauseCache.Seek(0, SeekOrigin.Begin);
                            Recording.PauseCache.CopyTo(buffer);
                            Recording.PauseCache = null;
                        }
                        buffer.Write(e.Buffer, 0, e.BytesRecorded);
                    }
                }
            };
            silence?.Play();
            capture.StartRecording();

            return Task.CompletedTask;
        });
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
