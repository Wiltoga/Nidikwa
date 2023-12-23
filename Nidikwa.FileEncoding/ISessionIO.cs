using Nidikwa.Models;

namespace Nidikwa.FileEncoding;

internal interface ISessionIO
{
    ushort FileVersion { get; }

    Task<RecordSessionMetadata> ReadMetadataAsync(Stream stream, CancellationToken cancellationToken);

    Task<RecordSession> ReadSessionAsync(Stream stream, CancellationToken cancellationToken);

    Task WriteSessionAsync(RecordSession recordSession, Stream stream, CancellationToken cancellationToken);
}