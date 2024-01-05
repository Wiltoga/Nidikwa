using Nidikwa.Common;
using Nidikwa.FileEncoding;
using Nidikwa.Models;

namespace Nidikwa.Sdk;

public static class QueueAccessor
{
    public static async Task<RecordSessionFile[]> GetQueueAsync(CancellationToken token = default)
    {
        NidikwaFiles.EnsureQueueFolderExists();
        var result = new List<RecordSessionFile>();
        var reader = new SessionEncoder();
        foreach (var file in Directory.GetFiles(NidikwaFiles.QueueFolder, "*.ndkw"))
        {
            using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var metadata = await reader.ParseMetadataAsync(fileStream, null, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                result.Add(new RecordSessionFile(metadata, file));
            }
            catch (FormatException) { }
            catch (ArgumentException) { }
        }

        return result.ToArray();
    }

    public static string GenerateFileName(RecordSessionMetadata metadata)
    {
        NidikwaFiles.EnsureQueueFolderExists();
        return Path.Combine(NidikwaFiles.QueueFolder, metadata.Id.ToString() + ".ndkw");
    }
}
