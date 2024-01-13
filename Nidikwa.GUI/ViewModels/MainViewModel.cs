using Nidikwa.Common;
using Nidikwa.Models;
using Nidikwa.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Shapes;
using TimeoutException = System.TimeoutException;

namespace Nidikwa.GUI.ViewModels
{
    public class MainViewModel : DestroyableReactiveObject
    {
        private const string host = "localhost";
        private const int port = 17854;

        [Reactive]
        public bool Connected { get; set; }

        public IControllerService? DeviceChangeEvent { get; set; }

        [Reactive]
        public DeviceViewModel[] Devices { get; set; }

        [Reactive]
        public bool Disconnected { get; set; }

        [Reactive]
        public int DurationSeconds { get; set; }

        [Reactive]
        public RecordSessionFile[] Queue { get; set; }

        public FileSystemWatcher? QueueWatcher { get; set; }

        [Reactive]
        public bool Recording { get; set; }

        public IControllerService? Service { get; set; }

        public IControllerService? StatusChangeEvent { get; set; }

        [Reactive]
        public bool TimeoutReached { get; set; }

        public MainViewModel()
        {
            Connected = false;
            TimeoutReached = false;
            this.WhenAnyValue(o => o.Connected)
                .Select(value => !value)
                .BindTo(this, o => o.Disconnected)
                .DestroyWith(this);
            Recording = false;
            Devices = [];
            Queue = [];
            DurationSeconds = 30;
        }

        public async Task AddQueueAsync()
        {
            if (Service is null)
                return;
            try
            {
                var result = await Service.GetStatusAsync(Token);
                if (result.Code == ResultCodes.Success)
                {
                    if (result.Data == RecordStatus.Recording)
                    {
                        await Service.SaveAsync(Token);
                    }
                }
                else
                {
                    MessageBox.Show(result.Code.ToString() + " : " + result.ErrorMessage, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task ConnectAsync()
        {
            await Task.WhenAll([
                ConnectMainServiceAsync(),
                ConnectDeviceServiceAsync(),
                ConnectStatusServiceAsync(),
            ]);
            Connected = true;
        }

        public async Task StartQueueWatcherAsync()
        {
            QueueAccessor.WatchQueue(async () =>
            {
                Queue = await QueueAccessor.GetQueueAsync(Token);
            }).DestroyWith(this);
            Queue = await QueueAccessor.GetQueueAsync(Token);
        }

        public async Task StartStopRecordAsync()
        {
            if (Service is null)
                return;
            try
            {
                var result = await Service.GetStatusAsync(Token);
                if (result.Code == ResultCodes.Success)
                {
                    if (result.Data == RecordStatus.Recording)
                    {
                        await Service.StopRecordingAsync(Token);
                    }
                    else
                    {
                        await Service.StartRecordingAsync(new RecordParams(Devices
                            .Where(device => device.Selected)
                            .Select(device => device.Reference.Id)
                            .ToArray(), TimeSpan.FromSeconds(DurationSeconds)), Token);
                    }
                }
                else
                {
                    MessageBox.Show(result.Code.ToString() + " : " + result.ErrorMessage, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task AutoCheckDevicesAsync(IControllerService controller)
        {
            if (Recording)
            {
                var recordedDevicesResult = await controller.GetRecordingDevicesAsync();
                if (recordedDevicesResult.Code != ResultCodes.Success)
                    return;
                foreach (var device in Devices)
                {
                    var recorded = recordedDevicesResult.Data?.Any(d => d.Id == device.Reference.Id) is true;
                    device.Selected = recorded;
                }
            }
        }

        private async Task AutoExtractDurationAsync(IControllerService controller)
        {
            if (Recording)
            {
                var recordedDurationResult = await controller.GetRecordingDurationAsync();
                if (recordedDurationResult.Code != ResultCodes.Success)
                    return;
                DurationSeconds = Math.Clamp((int)recordedDurationResult.Data.TotalSeconds, 5, 60);
            }
        }

        private async Task ConnectDeviceServiceAsync()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        DeviceChangeEvent = await ControllerService.ConnectAsync(host, port, Token);
                        DeviceChangeEvent.DestroyWith(this);
                        _ = Task.Run(() => DeviceLoop(DeviceChangeEvent));
                        break;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ConnectMainServiceAsync()
        {
            var errorShown = false;
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        Service = await ControllerService.ConnectAsync(host, port, Token);
                        Service.DestroyWith(this);
                        break;
                    }
                    catch (TimeoutException)
                    {
                        if (!errorShown)
                        {
                            if (!errorShown)
                            {
                                MessageBox.Show($"Unable to connect, the service is likely not started or listening on a port different than {port}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                errorShown = true;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ConnectStatusServiceAsync()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        StatusChangeEvent = await ControllerService.ConnectAsync(host, port, Token);
                        StatusChangeEvent.DestroyWith(this);
                        _ = Task.Run(() => StatusLoop(StatusChangeEvent));
                        break;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task DeviceLoop(IControllerService controller)
        {
            try
            {
                SetDevices(await DevicesAccessor.GetAvailableDevicesAsync());
                await AutoCheckDevicesAsync(controller);
                while (!Token.IsCancellationRequested)
                {
                    await controller.WaitDevicesChangedAsync(Token);
                    SetDevices(await DevicesAccessor.GetAvailableDevicesAsync());
                    await AutoCheckDevicesAsync(controller);
                }
            }
            catch (OperationCanceledException) { }
        }

        private void SetDevices(Device[] devices)
        {
            Devices = devices.Select(devices => new DeviceViewModel(devices)).ToArray();
        }

        private async Task StatusLoop(IControllerService controller)
        {
            try
            {
                var result = await controller.GetStatusAsync(Token);
                if (result.Code == Common.ResultCodes.Success)
                    Recording = result.Data == Common.RecordStatus.Recording;
                await AutoCheckDevicesAsync(controller);
                await AutoExtractDurationAsync(controller);
                while (!Token.IsCancellationRequested)
                {
                    await controller.WaitStatusChangedAsync(Token);
                    result = await controller.GetStatusAsync(Token);
                    if (result.Code == Common.ResultCodes.Success)
                        Recording = result.Data == Common.RecordStatus.Recording;
                    await AutoCheckDevicesAsync(controller);
                    await AutoExtractDurationAsync(controller);
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
