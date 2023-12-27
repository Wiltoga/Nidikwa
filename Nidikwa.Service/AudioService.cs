using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using Nidikwa.FileEncoding;
using Nidikwa.Models;
using Nidikwa.Service.Utilities;
using System.Linq;

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
    public bool Paused { get; set; }
    public MemoryStream? PauseCache { get; set; }
    public MMDevice MmDevice { get; }
    public WasapiCapture Capture { get; }
    public CacheStream Cache { get; }
    public BufferedStream Buffer { get; }
    public IWavePlayer? Silence { get; }
}

internal class AudioService : IAudioService, IMMNotificationClient, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly ILogger<AudioService> logger;
    private MMDeviceEnumerator MMDeviceEnumerator;
    private List<string> deviceCache;

    public event EventHandler? QueueChanged;
    public event EventHandler? StatusChanged;
    public event EventHandler? DevicesChanged;

    private DeviceRecording[]? Recordings { get; set; }

    public AudioService(
       ILogger<AudioService> logger
    )
    {
        MMDeviceEnumerator = new MMDeviceEnumerator();
        this.logger = logger;
        deviceCache = new();
        deviceCache.AddRange(MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device => device.ID));
        MMDeviceEnumerator.RegisterEndpointNotificationCallback(this);
    }

    public Task StopRecordAsync()
    {
        logger.LogInformation("Stop recording");
        return Locked(async () =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");
            var tasks = Recordings.Select(async recording =>
            {
                var taskSource = new TaskCompletionSource();
                recording.Capture.RecordingStopped += (sender, e) =>
                {
                    taskSource.SetResult();
                };
                recording.Capture.StopRecording();

                await taskSource.Task.ConfigureAwait(false);

                recording.Silence?.Stop();
                recording.Buffer.Flush();
                recording.Cache.Seek(0, SeekOrigin.Begin);
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Recordings = null;
        });
    }

    public Task<RecordSessionFile> AddToQueueAsync()
    {
        logger.LogInformation("Add to queue");
        return Locked(async () =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");

            var sessionsData = await Task.WhenAll(Recordings.Select(async (recording, index) =>
            {
                var cacheCopy = new MemoryStream();

                lock (recording.Mutex)
                {
                    recording.Paused = true;
                }
                recording.Buffer.Flush();
                recording.Cache.Seek(0, SeekOrigin.Begin);
                await recording.Cache.CopyToAsync(cacheCopy);
                recording.Cache.Seek(0, SeekOrigin.End);
                lock (recording.Mutex)
                {
                    recording.Paused = false;
                }
                return (cacheCopy, recording.MmDevice, recording.Capture.WaveFormat);
            })).ConfigureAwait(false);

            var duration = TimeSpan.FromSeconds(Recordings.First().Cache.Length / (double)sessionsData.First().WaveFormat.AverageBytesPerSecond);

            var deviceSessions = await Task.WhenAll(sessionsData.Select(async sessionData =>
            {
                using var waveBytes = new MemoryStream();
                using var writer = new WaveFileWriter(waveBytes, sessionData.WaveFormat);
                sessionData.cacheCopy.Seek(0, SeekOrigin.Begin);
                await sessionData.cacheCopy.CopyToAsync(writer);
                await writer.FlushAsync();
                sessionData.cacheCopy.Dispose();

                return new DeviceSession(
                    new Device(
                        sessionData.MmDevice.ID,
                        sessionData.MmDevice.FriendlyName,
                        sessionData.MmDevice.DataFlow == DataFlow.Render ? DeviceType.Output : DeviceType.Input
                    ),
                    waveBytes.ToArray()
                );
            })).ConfigureAwait(false);

            var resultFile = new RecordSession(
                new RecordSessionMetadata(
                    Guid.NewGuid(),
                    DateTimeOffset.Now,
                    duration
                ),
                deviceSessions
            );
            var writer = new SessionEncoder();
            NidikwaFiles.EnsureQueueFolderExists();
            var file = Path.Combine(NidikwaFiles.QueueFolder, $"{resultFile.Metadata.Id}.ndkw");
            using var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);

            await writer.WriteSessionAsync(resultFile, fileStream).ConfigureAwait(false);

            QueueChanged?.Invoke(this, EventArgs.Empty);

            return new RecordSessionFile(resultFile.Metadata, file);
        });
    }

    public Task StartRecordAsync(string[] ids)
    {
        logger.LogInformation("Start recording");
        return Locked(async () =>
        {
            if (Recordings is not null)
                throw new InvalidOperationException("The service is already recording");

            Recordings = ids.Select(id =>
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

                if (capture.WaveFormat.Channels > 2)
                    capture.WaveFormat = new WaveFormat(capture.WaveFormat.SampleRate, capture.WaveFormat.BitsPerSample, 2);

                var cache = new CacheStream(new MemoryStream(), capture.WaveFormat.AverageBytesPerSecond * 5);
                var buffer = new BufferedStream(cache, 1 << 20);
                var deviceRecording = new DeviceRecording(mmDevice, capture, cache, buffer, silence);
                capture.DataAvailable += (sender, e) =>
                {
                    lock (deviceRecording.Mutex)
                    {
                        if (deviceRecording.Paused)
                        {
                            if (deviceRecording.PauseCache is null)
                                deviceRecording.PauseCache = new MemoryStream();

                            deviceRecording.PauseCache.Write(e.Buffer, 0, e.BytesRecorded);
                        }
                        else
                        {
                            if (deviceRecording.PauseCache is not null)
                            {
                                deviceRecording.PauseCache.Seek(0, SeekOrigin.Begin);
                                deviceRecording.PauseCache.CopyTo(buffer);
                                deviceRecording.PauseCache = null;
                            }
                            buffer.Write(e.Buffer, 0, e.BytesRecorded);
                        }
                    }
                };
                silence?.Play();

                return deviceRecording;
            }).ToArray();

            await Task.WhenAll(Recordings.Select(recording => Task.Run(() => recording.Capture.StartRecording()))).ConfigureAwait(false);

            return Task.CompletedTask;
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

    public Task<bool> IsRecordingAsync()
    {
        return Locked(() =>
        {
            return Task.FromResult(Recordings is not null);
        });
    }

    public async Task DeleteQueueItemsAsync(Guid[] ids)
    {
        var sessionEncoder = new SessionEncoder();
        var mutex = new object();
        var deleted = false;
        await Task.WhenAll(Directory.GetFiles(NidikwaFiles.QueueFolder).Select(async file =>
        {
            try
            {
                RecordSessionMetadata metadata;
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    metadata = await sessionEncoder.ParseMetadataAsync(stream);
                }
                if (ids.Contains(metadata.Id))
                {
                    File.Delete(file);
                    lock (mutex)
                    {
                        deleted = true;
                    }
                }
            }
            catch { }
        })).ConfigureAwait(false);
        if (deleted)
            QueueChanged?.Invoke(this, EventArgs.Empty);
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

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        
    }

    public async ValueTask DisposeAsync()
    {
        if (await IsRecordingAsync())
            await StopRecordAsync();
        MMDeviceEnumerator.UnregisterEndpointNotificationCallback(this);
    }
}
