using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service;

internal interface IAudioService
{
    event EventHandler QueueChanged;
    event EventHandler StatusChanged;
    event EventHandler DevicesChanged;

    Task DeleteQueueItemsAsync(Guid[] ids);

    Task<bool> IsRecordingAsync();

    Task StartRecordAsync(RecordParams args);

    Task StopRecordAsync();

    Task<RecordSessionFile> AddToQueueAsync();
}
