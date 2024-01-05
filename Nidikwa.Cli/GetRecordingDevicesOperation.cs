using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("recording-devices", "rd", "Returns the list of the devices currently being recorded")]
internal class GetRecordingDevicesOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync(host, port);

            Console.Write(JsonConvert.SerializeObject(await instance.GetRecordingDevicesAsync(), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
