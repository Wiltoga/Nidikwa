using Newtonsoft.Json.Linq;
using Nidikwa.Common;
using Nidikwa.FileEncoding;
using Nidikwa.Models;

namespace Nidikwa.Sdk;

public static class QueueAccessor
{
    private static List<Action> QueueCallbacks { get; } = new();

    private static FileSystemWatcher? QueueWatcher { get; set; }

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
        result.Sort((left, right) => left.SessionMetadata.Date.CompareTo(right.SessionMetadata.Date));
        return result.ToArray();
    }

    public static string GenerateFileName(RecordSessionMetadata metadata)
    {
        NidikwaFiles.EnsureQueueFolderExists();
        return Path.Combine(NidikwaFiles.QueueFolder, metadata.Id.ToString() + ".ndkw");
    }

    private static void QueueWatcher_Callback(object sender, FileSystemEventArgs e)
    {
        lock(QueueCallbacks)
        {
            foreach (var callback in QueueCallbacks)
            {
                callback();
            }
        }
    }

    private class QueueWatcherCancel : IDisposable
    {
        public QueueWatcherCancel(Action callback)
        {
            Callback = callback;
        }

        public Action Callback { get; }

        public void Dispose()
        {
            var clearWatcher = false;
            lock (QueueCallbacks)
            {
                QueueCallbacks.Remove(Callback);
                if (QueueCallbacks.Count == 0)
                    clearWatcher = true;
            }

            if (clearWatcher && QueueWatcher is not null)
            {
                QueueWatcher.Created -= QueueWatcher_Callback;
                QueueWatcher.Deleted -= QueueWatcher_Callback;
                QueueWatcher.Dispose();

                QueueWatcher = null;
            }
        }
    }

    public static IDisposable WatchQueue(Action callback)
    {
        lock (QueueCallbacks)
        {
            QueueCallbacks.Add(callback);
        }
        if (QueueWatcher is null)
        {
            QueueWatcher = new FileSystemWatcher();
            QueueWatcher.Path = NidikwaFiles.QueueFolder;
            QueueWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            QueueWatcher.Filter = "*.ndkw";
            QueueWatcher.IncludeSubdirectories = false;
            QueueWatcher.Changed += QueueWatcher_Callback;
            QueueWatcher.Deleted += QueueWatcher_Callback;
            QueueWatcher.EnableRaisingEvents = true;
        }

        return new QueueWatcherCancel(callback);
    }
}
