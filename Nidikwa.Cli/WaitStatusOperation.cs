using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("wait-status", "wss", "Waits until the status of the service changes")]
internal class WaitStatusOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync();

            var result = await instance.WaitStatusChangedAsync();
            if (result.Code != Common.ResultCodes.Success)
            {
                Console.Write(JsonConvert.SerializeObject(result, IOperation.JsonSettings));
                return;
            }
            Console.Write(JsonConvert.SerializeObject(await instance.GetStatusAsync(), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
