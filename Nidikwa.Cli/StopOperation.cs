using Newtonsoft.Json;

namespace Nidikwa.Cli;

[Operation("stop", "st", "Stops the current recording")]
internal class StopOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        var instance = await SdkHandler.GetInstanceAsync();

        Console.Write(JsonConvert.SerializeObject(await instance.StopRecordingAsync()));
    }
}
