using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using Nidikwa.FileEncoding;
using Nidikwa.Models;
using Nidikwa.Common;
using System.IO;

namespace Nidikwa.Service;

internal class DeviceRecording
{
    public BufferedStream Buffer { get; }

    public CacheStream Cache { get; }

    public WasapiCapture Capture { get; }

    public MMDevice MmDevice { get; }

    public object Mutex { get; }

    public MemoryStream? PauseCache { get; set; }

    public bool Paused { get; set; }

    public IWavePlayer? Silence { get; }

    public string TempFile { get; }

    public DeviceRecording(
        MMDevice mmDevice,
        WasapiCapture capture,
        CacheStream cache,
        BufferedStream buffer,
        IWavePlayer? silence,
        string tempFile
    )
    {
        MmDevice = mmDevice;
        Capture = capture;
        Cache = cache;
        Buffer = buffer;
        Silence = silence;
        TempFile = tempFile;
        Paused = false;
        Mutex = new();
    }
}

internal class AudioService : IAudioService, IMMNotificationClient, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly ILogger<AudioService> logger;
    private bool activeRecording;
    private object activeRecordingMutex;
    private List<string> deviceCache;
    private MMDeviceEnumerator MMDeviceEnumerator;
    private TimeSpan CurrentMaxDuration { get; set; }

    private DeviceRecording[]? Recordings { get; set; }

    public AudioService(
       ILogger<AudioService> logger
    )
    {
        MMDeviceEnumerator = new MMDeviceEnumerator();
        this.logger = logger;
        deviceCache = new();
        activeRecording = false;
        activeRecordingMutex = new();
        deviceCache.AddRange(MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device => device.ID));
        MMDeviceEnumerator.RegisterEndpointNotificationCallback(this);
    }

    public event EventHandler? DevicesChanged;

    public event EventHandler? StatusChanged;

    public async ValueTask DisposeAsync()
    {
        if (await IsRecordingAsync())
            await StopRecordAsync();
        MMDeviceEnumerator.UnregisterEndpointNotificationCallback(this);
    }

    public Task<TimeSpan> GetCurrentMaxDurationAsync()
    {
        return Locked(() =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");

            return Task.FromResult(CurrentMaxDuration);
        });
    }

    public Task<Device[]> GetRecordingDevicesAsync()
    {
        return Locked(() =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");

            return Task.FromResult(Recordings.Select(session => MapDevice(session.MmDevice)).ToArray());
        });
    }

    public Task<bool> IsRecordingAsync()
    {
        return Locked(() =>
        {
            return Task.FromResult(Recordings is not null);
        });
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {

    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        var device = MMDeviceEnumerator.GetDevice(pwstrDeviceId);
        if (device.State == DeviceState.Active)
        {
            deviceCache.Add(pwstrDeviceId);
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        if (deviceCache.Remove(deviceId))
        {
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (deviceCache.Contains(deviceId) && newState != DeviceState.Active)
        {
            deviceCache.Remove(deviceId);
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (!deviceCache.Contains(deviceId) && newState == DeviceState.Active)
        {
            deviceCache.Add(deviceId);
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {

    }

    public async Task<Stream> SaveAsNdkwAsync()
    {
        logger.LogInformation("Save as NDKW");
        var (sessionsData, duration) = await Locked(async () =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");

            TimeSpan? duration = null;
            var sessionsData = await Task.WhenAll(Recordings.Select(async (recording, index) =>
            {
                lock (recording.Mutex)
                {
                    recording.Paused = true;
                }
                duration ??= TimeSpan.FromSeconds(recording.Cache.Length / (double)recording.Capture.WaveFormat.AverageBytesPerSecond);
                recording.Buffer.Flush();
                var tempFile = Path.GetTempFileName();

                recording.Cache.Seek(0, SeekOrigin.Begin);
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    var waveWriter = new WaveFileWriter(fileStream, recording.Capture.WaveFormat);
                    await recording.Cache.CopyToAsync(waveWriter);
                    await waveWriter.FlushAsync();
                }
                recording.Cache.Seek(0, SeekOrigin.End);
                lock (recording.Mutex)
                {
                    recording.Paused = false;
                }
                return (tempFile, recording.MmDevice);
            })).ConfigureAwait(false);

            return (sessionsData, duration);
        });
        
        var deviceSessions = sessionsData.Select(sessionData => new DeviceSessionAsFile(MapDevice(sessionData.MmDevice), sessionData.tempFile)).ToArray();

        var resultFile = new RecordSessionAsFile(
            new RecordSessionMetadata(
                Guid.NewGuid(),
                DateTimeOffset.Now,
                duration!.Value
            ),
            deviceSessions
        );

        var encoder = new SessionEncoder();
        var stream = new EchoStream();
        _ = encoder.StreamSessionAsync(resultFile, stream);
        return stream;
    }

    public Task StartRecordAsync(RecordParams args)
    {
        logger.LogInformation("Start recording");
        if (args.DeviceIds.Length == 0)
            return Task.CompletedTask;
        return Locked(async () =>
        {
            if (Recordings is not null)
                throw new InvalidOperationException("The service is already recording");

            Recordings = args.DeviceIds.Select(id =>
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

                capture.WaveFormat = new WaveFormat(capture.WaveFormat.SampleRate, 32, capture.WaveFormat.Channels);
                if (capture.WaveFormat.Channels > 2)
                    capture.WaveFormat = new WaveFormat(capture.WaveFormat.SampleRate, capture.WaveFormat.BitsPerSample, 2);

                var tempFilename = Path.GetTempFileName();
                var cache = new CacheStream(new FileStream(tempFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read), capture.WaveFormat.AverageBytesPerSecond * (long)args.CacheDuration.TotalSeconds);
                var buffer = new BufferedStream(cache, 1 << 23);
                var deviceRecording = new DeviceRecording(mmDevice, capture, cache, buffer, silence, tempFilename);
                void writeBuffer(ReadOnlySpan<byte> recordedBuffer)
                {
                    if (deviceRecording.Paused)
                    {
                        if (deviceRecording.PauseCache is null)
                            deviceRecording.PauseCache = new MemoryStream();

                        deviceRecording.PauseCache.Write(recordedBuffer);
                    }
                    else
                    {
                        if (deviceRecording.PauseCache is not null)
                        {
                            deviceRecording.PauseCache.Seek(0, SeekOrigin.Begin);
                            deviceRecording.PauseCache.CopyTo(buffer);
                            deviceRecording.PauseCache = null;
                        }
                        buffer.Write(recordedBuffer);
                    }
                }
                capture.DataAvailable += (sender, e) =>
                {
                    lock (activeRecordingMutex)
                    {
                        if (!activeRecording)
                            return;
                    }
                    lock (deviceRecording.Mutex)
                    {
                        writeBuffer(e.Buffer[..e.BytesRecorded]);
                    }
                };
                silence?.Play();

                return deviceRecording;
            }).ToArray();

            await Task.WhenAll(Recordings.Select(recording => Task.Run(() => recording.Capture.StartRecording()))).ConfigureAwait(false);

            lock (activeRecordingMutex)
            {
                activeRecording = true;
            }
            CurrentMaxDuration = args.CacheDuration;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    public Task StopRecordAsync()
    {
        logger.LogInformation("Stop recording");
        return Locked(async () =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");
            lock (activeRecordingMutex)
            {
                activeRecording = true;
            }
            var tasks = Recordings.Select(async recording =>
            {
                var taskSource = new TaskCompletionSource();
                recording.Capture.RecordingStopped += (sender, e) =>
                {
                    taskSource.SetResult();
                };
                recording.Capture.StopRecording();

                await taskSource.Task.ConfigureAwait(false);

                recording.Capture.Dispose();

                recording.Silence?.Stop();
                recording.Buffer.Dispose();

                File.Delete(recording.TempFile);
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);
            StatusChanged?.Invoke(this, EventArgs.Empty);

            Recordings = null;
        });
    }

    private async Task Locked(Func<Task> action)
    {
        try
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            await action().ConfigureAwait(false);
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
            await _lock.WaitAsync().ConfigureAwait(false);
            return await action().ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Device MapDevice(MMDevice device)
    {
        return new Device(
                    device.ID,
                    device.FriendlyName,
                    device.DataFlow == DataFlow.Render ? DeviceType.Output : DeviceType.Input
                );
    }
}
