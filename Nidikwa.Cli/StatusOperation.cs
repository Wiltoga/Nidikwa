using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("status", "ss", "Returns the current status of the service")]
internal class StatusOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync(host, port);

            Console.Write(JsonConvert.SerializeObject(await instance.GetStatusAsync(), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
