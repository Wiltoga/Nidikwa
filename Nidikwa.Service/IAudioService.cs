using Nidikwa.Models;
using Nidikwa.Common;

namespace Nidikwa.Service;

internal interface IAudioService
{
    event EventHandler StatusChanged;
    event EventHandler DevicesChanged;

    Task<bool> IsRecordingAsync();

    Task StartRecordAsync(RecordParams args);

    Task StopRecordAsync();

    Task<ReadOnlyMemory<byte>> SaveAsNdkwAsync();
}
