using Nidikwa.Models;

namespace Nidikwa.Service;

public class DeviceChangedEventArgs : EventArgs
{
    public required Device[] Devices { get; init; }
}

public enum AudioServiceStatus
{
    Recording,
    Stopped,
}

public class StatusChangedEventArgs : EventArgs
{
    public required AudioServiceStatus Status { get; init; }
}

public delegate void DevicesChangedEventHandler(object sender, DeviceChangedEventArgs e);
public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

public interface IAudioService
{
    event StatusChangedEventHandler StatusChanged;
    event DevicesChangedEventHandler DevicesChanged;

    AudioServiceStatus Status { get; }

    void StartRecord(string[] DeviceIds, TimeSpan CacheDuration);

    Device[] GetRecordingDevices();

    Device[] GetAllDevices();

    Device GetDefaultDevice(DeviceType type);

    TimeSpan GetCurrentMaxDuration();

    void StopRecord();

    Task SaveAsNdkwAsync(Stream stream);
}
