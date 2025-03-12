using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using Nidikwa.FileEncoding;
using Nidikwa.Models;

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

internal sealed class AudioService : IAudioService, IMMNotificationClient, IDisposable
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly ILogger<AudioService> logger;
    private bool activeRecording;
    private object activeRecordingMutex;
    private List<Device> deviceCache;
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
        deviceCache.AddRange(MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(MapDevice));
        MMDeviceEnumerator.RegisterEndpointNotificationCallback(this);
    }

    public event DevicesChangedEventHandler? DevicesChanged;

    public event StatusChangedEventHandler? StatusChanged;

    public void Dispose()
    {
        if (Status == AudioServiceStatus.Recording)
            StopRecord();
        MMDeviceEnumerator.UnregisterEndpointNotificationCallback(this);
    }

    public TimeSpan GetCurrentMaxDuration()
    {
        return Locked(() =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");

            return CurrentMaxDuration;
        });
    }

    public Device[] GetRecordingDevices()
    {
        return Locked(() =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");

            return Recordings.Select(session => MapDevice(session.MmDevice)).ToArray();
        });
    }

    public Device[] GetAllDevices()
    {
        return deviceCache.ToArray();
    }

    public Device GetDefaultDevice(DeviceType type)
    {
        var device = MMDeviceEnumerator.GetDefaultAudioEndpoint(type == DeviceType.Input ? DataFlow.Capture : DataFlow.Render, Role.Multimedia);
        return deviceCache.Find(d => d.Id == device.ID)!;
    }

    public AudioServiceStatus Status
    {
        get
        {
            return Locked(() =>
            {
                return Recordings is null ? AudioServiceStatus.Stopped : AudioServiceStatus.Recording;
            });
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {

    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        var device = MMDeviceEnumerator.GetDevice(pwstrDeviceId);
        if (device.State == DeviceState.Active)
        {
            deviceCache.Add(MapDevice(device));
            DevicesChanged?.Invoke(this, new DeviceChangedEventArgs { Devices = deviceCache.ToArray() });
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        if (deviceCache.RemoveAll(device => device.Id == deviceId) > 0)
        {
            DevicesChanged?.Invoke(this, new DeviceChangedEventArgs { Devices = deviceCache.ToArray() });
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        if (deviceCache.Exists(device => device.Id == deviceId) && newState != DeviceState.Active)
        {
            var device = deviceCache.Find(device => device.Id == deviceId)!;
            deviceCache.Remove(device);
            DevicesChanged?.Invoke(this, new DeviceChangedEventArgs { Devices = deviceCache.ToArray() });
        }
        else if (!deviceCache.Exists(device => device.Id == deviceId) && newState == DeviceState.Active)
        {
            var device = MapDevice(MMDeviceEnumerator.GetDevice(deviceId));
            deviceCache.Add(device);
            DevicesChanged?.Invoke(this, new DeviceChangedEventArgs { Devices = deviceCache.ToArray() });
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {

    }

    public async Task SaveAsNdkwAsync(Stream stream)
    {
        logger.LogInformation("Save as NDKW");
        await Locked(async () =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");
            logger.LogInformation("Stopping recordings");

            foreach (var recording in Recordings)
            {
                lock (recording.Mutex)
                {
                    recording.Paused = true;
                }
            }

            logger.LogInformation("Exporting to RecordSession");
            TimeSpan duration = default;
            var sessionsData = Recordings.ToDictionary(recording => recording.MmDevice.ID, recording =>
            {
                recording.Buffer.Flush();

                recording.Cache.Seek(0, SeekOrigin.Begin);

                var waveStream = new RawSourceWaveStream(recording.Cache, recording.Capture.WaveFormat);
                var waveFileStream = new WaveFileStream(waveStream);
                duration = waveStream.TotalTime;

                return waveFileStream as Stream;
            });

            var recordSession = new RecordSession(
                new RecordSessionMetadata(
                    DateTimeOffset.Now,
                    duration,
                    Recordings.Select(recording => MapDevice(recording.MmDevice)).ToArray()
                ),
                sessionsData
            );

            var encoder = new SessionEncoder();
            logger.LogInformation("Saving session");
            await encoder.WriteAsync(stream, recordSession).ConfigureAwait(false);

            logger.LogInformation("Restarting recordings");

            foreach (var recording in Recordings)
            {
                recording.Cache.Seek(0, SeekOrigin.End);
                lock (recording.Mutex)
                {
                    recording.Paused = false;
                }
            }
        }).ConfigureAwait(false);
    }

    public void StartRecord(string[] deviceIds, TimeSpan cacheDuration)
    {
        logger.LogInformation("Start recording");
        if (deviceIds.Length == 0)
            return;
        Locked(() =>
        {
            if (Recordings is not null)
                throw new InvalidOperationException("The service is already recording");

            Recordings = deviceIds.Select(id =>
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
                var cache = new CacheStream(new FileStream(tempFilename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read), capture.WaveFormat.AverageBytesPerSecond * (long)cacheDuration.TotalSeconds);
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
                        writeBuffer(e.Buffer.AsSpan()[..e.BytesRecorded]);
                    }
                };
                silence?.Play();

                return deviceRecording;
            }).ToArray();

            foreach (var recording in Recordings)
            {
                recording.Capture.StartRecording();
            }

            lock (activeRecordingMutex)
            {
                activeRecording = true;
            }
            CurrentMaxDuration = cacheDuration;
            StatusChanged?.Invoke(this, new StatusChangedEventArgs { Status = Status });
        });
    }

    public void StopRecord()
    {
        logger.LogInformation("Stop recording");
        Locked(() =>
        {
            if (Recordings is null)
                throw new InvalidOperationException("The service is not recording");
            lock (activeRecordingMutex)
            {
                activeRecording = false;
            }
            var recordingSets = Recordings.Select(recording =>
            {
                var mutex = new SemaphoreSlim(0, 1);
                recording.Capture.RecordingStopped += (sender, e) =>
                {
                    mutex.Release();
                };
                recording.Capture.StopRecording();

                return (recording, mutex);
            }).ToArray();

            foreach (var (recording, mutex) in recordingSets)
            {
                mutex.Wait();
                recording.Capture.Dispose();

                recording.Silence?.Stop();
                recording.Buffer.Dispose();

                File.Delete(recording.TempFile);
            }
            
            Recordings = null;

            StatusChanged?.Invoke(this, new StatusChangedEventArgs { Status = Status });

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

    private T Locked<T>(Func<T> action)
    {
        try
        {
            _lock.Wait();
            return action();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void Locked(Action action)
    {
        try
        {
            _lock.Wait();
            action();
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
