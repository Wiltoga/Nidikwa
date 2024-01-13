using NAudio.CoreAudioApi;
using Nidikwa.Models;

namespace Nidikwa.Sdk;

public static class DevicesAccessor
{
    private static MMDeviceEnumerator? _enumerator;
    internal static MMDeviceEnumerator Enumerator => _enumerator ??= new MMDeviceEnumerator();
    public static Task<Device[]> GetAvailableDevicesAsync()
    {
        return Task.Run(() => Enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active)
            .Select(device => new Device(device.ID, device.FriendlyName, device.DataFlow == DataFlow.Render ? DeviceType.Output : DeviceType.Input))
            .ToArray());
    }
    public static async Task<Device> GetDefaultOutputDeviceAsync()
    {
        var mmDevice = await Task.Run(() => Enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)).ConfigureAwait(false);
        return new Device(mmDevice.ID, mmDevice.FriendlyName, DeviceType.Output);
    }
    public static async Task<Device> GetDefaultInputDeviceAsync()
    {
        var mmDevice = await Task.Run(() => Enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)).ConfigureAwait(false);
        return new Device(mmDevice.ID, mmDevice.FriendlyName, DeviceType.Input);
    }
}
