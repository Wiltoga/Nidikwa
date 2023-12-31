﻿using Newtonsoft.Json;
using Nidikwa.Common;
using Nidikwa.Models;

namespace Nidikwa.Cli;

[Operation("wait-list-queue", "wlq", "Waits until the queue of the service changes")]
internal class WaitQueueOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync(host, port);

            var result = await instance.WaitQueueChangedAsync();
            if (result.Code != Common.ResultCodes.Success)
            {
                Console.Write(JsonConvert.SerializeObject(result, IOperation.JsonSettings));
                return;
            }
            Console.Write(JsonConvert.SerializeObject(new Result<RecordSessionMetadata[]>
            {
                Code = ResultCodes.Success,
                Data = (await QueueAccessor.GetQueueAsync()).Select(item => item.SessionMetadata).ToArray(),
            }, IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
