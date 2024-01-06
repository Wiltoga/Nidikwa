using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("max-duration", "md", "Returns the max duration of the current recording session")]
internal class GetMaxDurationOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        try
        {
            var instance = await SdkHandler.GetInstanceAsync(host, port);

            Console.Write(JsonConvert.SerializeObject(await instance.GetRecordingDurationAsync(), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
