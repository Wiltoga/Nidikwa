using NAudio.CoreAudioApi;
using NAudio.Wave;
using Newtonsoft.Json;
using Nidikwa.Models;

namespace Nidikwa.Service;

internal class AudioService : IAudioService
{
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly IConfiguration configuration;
    private MMDeviceEnumerator MMDeviceEnumerator;

    public AudioService(
       ILogger<AudioService> logger,
       IConfiguration configuration
    )
    {
        MMDeviceEnumerator = new MMDeviceEnumerator();
        this.configuration = configuration;
    }

    public Task<Device[]> GetAvailableDevicesAsync()
    {
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
        return Locked(() =>
        {
            var mmeDevice = MMDeviceEnumerator
                .GetDevice(id);
            if (mmeDevice is null)
                throw new KeyNotFoundException("Uknown device");

            return Task.FromResult(new Device(mmeDevice.ID, mmeDevice.FriendlyName, mmeDevice.DataFlow == DataFlow.Capture ? DeviceType.Input : DeviceType.Output));
        });
    }

    public Task StartRecordAsync(string id)
    {
        var mmeDevice = MMDeviceEnumerator
            .GetDevice(id);
        WasapiCapture capture;
        if (mmeDevice.DataFlow == DataFlow.Render)
        {
            capture = new WasapiLoopbackCapture(mmeDevice);
        }
        else
        {
            capture = new WasapiCapture(mmeDevice);
        }

        return Task.CompletedTask;
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
