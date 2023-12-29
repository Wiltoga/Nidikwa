using Nidikwa.FileEncoding;
using Nidikwa.Common;

namespace Nidikwa.Utilities;

public static class QueueAccessor
{
    public static async Task<RecordSessionFile[]> GetQueueAsync(CancellationToken token = default)
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
                token.ThrowIfCancellationRequested();

                result.Add(new RecordSessionFile(metadata, file));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch { }
        }

        return result.ToArray();
    }
}
