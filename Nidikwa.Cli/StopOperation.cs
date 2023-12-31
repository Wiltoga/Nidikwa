using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("stop", "st", "Stops the current recording")]
internal class StopOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync(host, port);

            Console.Write(JsonConvert.SerializeObject(await instance.StopRecordingAsync(), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
