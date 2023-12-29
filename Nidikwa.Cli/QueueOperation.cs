using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("queue", "q", "Enqueues the current recording")]
internal class QueueOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync();

            Console.Write(JsonConvert.SerializeObject(await instance.AddToQueueAsync(), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
