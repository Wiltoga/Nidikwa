using Nidikwa.Common;
using Nidikwa.Models;
using Nidikwa.Sdk;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;
using System.Windows;

namespace Nidikwa.GUI.ViewModels
{
    public class MainViewModel : DestroyableReactiveObject
    {
        [Reactive]
        public bool Connected { get; set; }
        [Reactive]
        public bool Recording { get; set; }
        [Reactive]
        public DeviceViewModel[] Devices { get; set; }
        [Reactive]
        public RecordSessionFile[] Queue { get; set; }

        public IControllerService? Service { get; set; }
        public IControllerService? DeviceChangeEvent { get; set; }
        public IControllerService? QueueChangeEvent { get; set; }
        public IControllerService? StatusChangeEvent { get; set; }

        public MainViewModel()
        {
            Connected = false;
            Recording = false;
            Devices = [];
            Queue = [];
        }

        private void SetDevices(Device[] devices)
        {
            Devices = devices.Select(devices => new DeviceViewModel(devices)).ToArray();
        }

        private async Task DeviceLoop(IControllerService controller)
        {
            try
            {
                SetDevices(await DevicesAccessor.GetAvailableDevicesAsync());
                while (!Token.IsCancellationRequested)
                {
                    await controller.WaitDevicesChangedAsync(Token);
                    SetDevices(await DevicesAccessor.GetAvailableDevicesAsync());
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task StatusLoop(IControllerService controller)
        {
            try
            {
                var result = await controller.GetStatusAsync(Token);
                if (result.Code == Common.ResultCodes.Success)
                    Recording = result.Data == Common.RecordStatus.Recording;
                while (!Token.IsCancellationRequested)
                {
                    await controller.WaitStatusChangedAsync(Token);
                    result = await controller.GetStatusAsync(Token);
                    if (result.Code == Common.ResultCodes.Success)
                        Recording = result.Data == Common.RecordStatus.Recording;
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task QueueLoop(IControllerService controller)
        {
            try
            {
                Queue = await QueueAccessor.GetQueueAsync(Token);
                while (!Token.IsCancellationRequested)
                {
                    await controller.WaitStatusChangedAsync(Token);
                    Queue = await QueueAccessor.GetQueueAsync(Token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ConnectMainServiceAsync()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        Service = await ControllerService.ConnectAsync(Token);
                        Service.DestroyWith(this);
                        break;
                    }
                    catch (TimeoutException)
                    {
                        if (MessageBox.Show("Service not started, do you want to start it now ?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            new Process
                            {
                                StartInfo = new ProcessStartInfo("Nidikwa.Service.exe")
                                {
                                    UseShellExecute = true
                                }
                            }.Start();
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ConnectDeviceServiceAsync()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        DeviceChangeEvent = await ControllerService.ConnectAsync(Token);
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

        private async Task ConnectStatusServiceAsync()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        StatusChangeEvent = await ControllerService.ConnectAsync(Token);
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

        private async Task ConnectQueueServiceAsync()
        {
            try
            {
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        QueueChangeEvent = await ControllerService.ConnectAsync(Token);
                        QueueChangeEvent.DestroyWith(this);
                        _ = Task.Run(() => QueueLoop(QueueChangeEvent));
                        break;
                    }
                    catch (TimeoutException)
                    {
                    }
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
                ConnectQueueServiceAsync(),
            ]);
            Connected = true;
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
                            .ToArray(), TimeSpan.FromSeconds(15)), Token);
                    }
                }
                else
                {
                    MessageBox.Show(result.Code.ToString() + " : " + result.ErrorMessage, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
