using Newtonsoft.Json;
using Nidikwa.Common;

namespace Nidikwa.Cli;

[Operation("record", "r", "Starts recording using the given deviceIds as parameters")]
internal class RecordOperation : IOperation
{
    public async Task ExecuteAsync(string[] args)
    {
        if (args.Length <= 1)
        {
            Console.WriteLine($"""
                Usage : record <duration> [<deviceIds>]
                Example : record 00:05:00 {Guid.Empty:B}.{Guid.NewGuid():B}
                """);
            return;
        }
        var duration = TimeSpan.Parse(args[0]);
        try
        {
            var instance = await SdkHandler.GetInstanceAsync();

            Console.Write(JsonConvert.SerializeObject(await instance.StartRecordingAsync(new RecordParams(args.Skip(1).ToArray(), duration)), IOperation.JsonSettings));
        }
        catch (TimeoutException)
        {
            Console.Write(JsonConvert.SerializeObject(new Result { Code = ResultCodes.Timeout }, IOperation.JsonSettings));
        }
    }
}
