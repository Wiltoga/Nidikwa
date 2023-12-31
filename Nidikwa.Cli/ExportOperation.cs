using Newtonsoft.Json;
using Nidikwa.Common;
using Nidikwa.FileEncoding;
using Nidikwa.Models;

namespace Nidikwa.Cli;

[Operation("export", "e", "Exports the selected queued item to an usable folder")]
internal class ExportOperation : IOperation
{
    public async Task ExecuteAsync(string host, int port, string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine($"""
                Usage : export <item id> <foldername>
                Example : export {Guid.NewGuid()} someFolder
                """);
            return;
        }
        var itemId = Guid.Parse(args[0]);
        var folderName = args[1];

        var queue = await QueueAccessor.GetQueueAsync();
        var itemMetadata = queue.FirstOrDefault(item => item.SessionMetadata.Id == itemId);

        if (itemMetadata is null)
        {
            Console.Write(JsonConvert.SerializeObject(new Result
            {
                Code = ResultCodes.NotFound,
                ErrorMessage = "No queued item found with this id",
            }, IOperation.JsonSettings));
            return;
        }
        var decoder = new SessionEncoder();
        RecordSession data;
        using (var stream = new FileStream(itemMetadata.File, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            data = await decoder.ParseSessionAsync(stream);
        }

        Directory.CreateDirectory(folderName);

        var itemFolder = Path.Combine(folderName, data.Metadata.Id.ToString());

        Directory.CreateDirectory(itemFolder);

        using (var stream = new StreamWriter(Path.Combine(itemFolder, "data.json")))
        {
            stream.Write(JsonConvert.SerializeObject(data.Metadata, IOperation.JsonSettings));
        }

        await Task.WhenAll(data.DeviceSessions.ToArray().Select(device => Task.Run(() =>
        {
            var deviceFolder = Path.Combine(itemFolder, device.Device.Id.ToString());
            Directory.CreateDirectory(deviceFolder);

            using (var stream = new StreamWriter(Path.Combine(deviceFolder, "data.json")))
            {
                stream.Write(JsonConvert.SerializeObject(device.Device, IOperation.JsonSettings));
            }

            using (var stream = new FileStream(Path.Combine(deviceFolder, "data.wav"), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.Write(device.WaveData.Span);
            }
        })));

        Console.Write(JsonConvert.SerializeObject(new Result
        {
            Code = ResultCodes.Success,
        }, IOperation.JsonSettings));
    }
}
