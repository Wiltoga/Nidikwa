using NAudio.CoreAudioApi;
using Nidikwa.Models;

namespace Nidikwa.Service;

internal class AudioService : IAudioService
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private MMDeviceEnumerator MMDeviceEnumerator;

    public AudioService(
       ILogger<AudioService> logger
    )
    {
        MMDeviceEnumerator = new MMDeviceEnumerator();
    }

    public Task<Device[]> GetAvailableDevices()
    {
        return Locked(() =>
        {
            return Task.FromResult(MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).Select(device =>
            {
                return new Device(device.ID, device.FriendlyName, device.DataFlow == DataFlow.Capture ? DeviceType.Input : DeviceType.Output);
            }).ToArray());
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
