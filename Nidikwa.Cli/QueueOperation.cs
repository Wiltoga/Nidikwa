using Newtonsoft.Json;

namespace Nidikwa.Cli;

[Operation("queue", "q", "Enqueues the current recording")]
internal class QueueOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        var instance = await SdkHandler.GetInstanceAsync();

        Console.Write(JsonConvert.SerializeObject(await instance.AddToQueueAsync(), IOperation.JsonSettings));
    }
}
