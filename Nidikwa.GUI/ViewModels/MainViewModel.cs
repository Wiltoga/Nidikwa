﻿using Nidikwa.Common;
using Nidikwa.Models;
using Nidikwa.Sdk;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Shapes;

namespace Nidikwa.GUI.ViewModels
{
    public class MainViewModel : DestroyableReactiveObject
    {
        private const string host = "localhost";
        private const int port = 17854;
        [Reactive]
        public bool Connected { get; set; }
        [Reactive]
        public bool Recording { get; set; }
        [Reactive]
        public DeviceViewModel[] Devices { get; set; }
        [Reactive]
        public RecordSessionFile[] Queue { get; set; }
        [Reactive]
        public TimeSpan Duration { get; set; }

        public IControllerService? Service { get; set; }
        public IControllerService? DeviceChangeEvent { get; set; }
        public IControllerService? StatusChangeEvent { get; set; }
        public FileSystemWatcher? QueueWatcher { get; set; }

        public MainViewModel()
        {
            Connected = false;
            Recording = false;
            Devices = [];
            Queue = [];
            Duration = TimeSpan.FromSeconds(30);
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

        private async Task StatusLoop(IControllerService controller)
        {
            try
            {
                var result = await controller.GetStatusAsync(Token);
                if (result.Code == Common.ResultCodes.Success)
                    Recording = result.Data == Common.RecordStatus.Recording;
                await AutoCheckDevicesAsync(controller);
                while (!Token.IsCancellationRequested)
                {
                    await controller.WaitStatusChangedAsync(Token);
                    result = await controller.GetStatusAsync(Token);
                    if (result.Code == Common.ResultCodes.Success)
                        Recording = result.Data == Common.RecordStatus.Recording;
                    await AutoCheckDevicesAsync(controller);
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
                            MessageBox.Show($"Unable to connect, the service is likely not started or listening on a port different than {port}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            errorShown = true;
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

        public async Task StartQueueWatcherAsync()
        {
            QueueWatcher = new FileSystemWatcher();
            QueueWatcher.Path = NidikwaFiles.QueueFolder;
            QueueWatcher.NotifyFilter = NotifyFilters.LastWrite;
            QueueWatcher.Filter = "*.ndkw";
            QueueWatcher.Changed += QueueWatcher_Changed;
            QueueWatcher.EnableRaisingEvents = true;
            Queue = await QueueAccessor.GetQueueAsync(Token);

            DisposeWhenDestroyed(() =>
            {
                QueueWatcher.Changed -= QueueWatcher_Changed;
                QueueWatcher.Dispose();
            });
        }

        private async void QueueWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Queue = await QueueAccessor.GetQueueAsync(Token);
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
                            .ToArray(), Duration), Token);
                    }
                }
                else
                {
                    MessageBox.Show(result.Code.ToString() + " : " + result.ErrorMessage, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException) { }
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
    }
}
