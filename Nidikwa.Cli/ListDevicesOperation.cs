using Newtonsoft.Json;
using Nidikwa.Common;
using Nidikwa.Models;
using Nidikwa.Sdk;

namespace Nidikwa.Cli;

[Operation("list-devices", "ld", "Displays the list of available devices")]
internal class ListDevicesOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("""
                Usage : list-devices <type> [--default]
                Where <type> is either "input", "output", "both"
                Where --default is optional and is used to only give the defaut device used for <type>
                """);
            return;
        }
        Device[] devices = [];
        if (args.Length >= 2 && args[1] == "--default")
        {
            if (args[0] == "input" || args[0] == "all")
                devices = [..devices, await DevicesAccessor.GetDefaultInputDeviceAsync()];
            if (args[0] == "output" || args[0] == "all")
                devices = [..devices, await DevicesAccessor.GetDefaultOutputDeviceAsync()];
        }
        else
        {
            devices = await DevicesAccessor.GetAvailableDevicesAsync();
            if (args[0] == "input")
                devices = devices.Where(device => device.Type == DeviceType.Input).ToArray();
            if (args[0] == "output")
                devices = devices.Where(device => device.Type == DeviceType.Output).ToArray();
        }
        Console.Write(JsonConvert.SerializeObject(new Result<Device[]>
        {
            Code = ResultCodes.Success,
            Data = devices,
        }, IOperation.JsonSettings));
    }
}
