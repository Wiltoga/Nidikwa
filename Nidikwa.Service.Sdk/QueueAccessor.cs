using Nidikwa.FileEncoding;

namespace Nidikwa.Service.Utilities;

public static class QueueAccessor
{
    public static async Task<RecordSessionFile[]> GetQueueAsync(CancellationToken token)
    {
        NidikwaFiles.EnsureQueueFolderExists();
        var result = new List<RecordSessionFile>();
        var reader = new SessionEncoder();
        foreach (var file in Directory.GetFiles(NidikwaFiles.QueueFolder))
        {
            using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var metadata = await reader.ParseMetadataAsync(fileStream, null, token);

                result.Add(new RecordSessionFile(metadata, file));
            }
            catch { }
        }

        return result.ToArray();
    } 
}
