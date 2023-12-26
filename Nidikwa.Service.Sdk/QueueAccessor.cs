using Nidikwa.FileEncoding;

namespace Nidikwa.Service.Utilities;

public static class QueueAccessor
{
    public static async Task<RecordSessionFile[]> GetQueueAsync(CancellationToken token)
    {
        NidikwaFiles.EnsureFolderExists();
        var result = new List<RecordSessionFile>();
        var reader = new SessionEncoder();
        foreach (var file in Directory.GetFiles(NidikwaFiles.QueueFolder))
        {
            var fileInfo = new FileInfo(file);
            using var fileStream = fileInfo.OpenRead();
            try
            {
                var metadata = await reader.ParseMetadataAsync(fileStream, null, token);

                result.Add(new RecordSessionFile(metadata, fileInfo));
            }
            catch { }
        }

        return result.ToArray();
    } 
}
