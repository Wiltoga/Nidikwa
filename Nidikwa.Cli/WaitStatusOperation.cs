﻿using Newtonsoft.Json;

namespace Nidikwa.Cli;

[Operation("wait-status", "wss", "Waits until the status of the service changes")]
internal class WaitStatusOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        var instance = await SdkHandler.GetInstanceAsync();

        var result = await instance.WaitStatusChangedAsync();
        if (result.Code != Common.ResultCodes.Success)
        {
            Console.Write(JsonConvert.SerializeObject(result));
            return;
        }
        Console.Write(JsonConvert.SerializeObject(await instance.GetStatusAsync()));
    }
}
