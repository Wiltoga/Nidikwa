using Newtonsoft.Json;
using Nidikwa.Common;
using Nidikwa.Models;
using Nidikwa.Sdk;

namespace Nidikwa.Cli;

[Operation("list-queue", "lq", "Displays the queue of the service")]
internal class ListQueueOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        Console.Write(JsonConvert.SerializeObject(new Result<RecordSessionMetadata[]>
        {
            Code = ResultCodes.Success,
            Data = (await QueueAccessor.GetQueueAsync()).Select(item => item.SessionMetadata).ToArray(),
        }, IOperation.JsonSettings));
    }
}
