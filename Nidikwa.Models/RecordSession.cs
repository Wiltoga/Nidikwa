namespace Nidikwa.Models;

public record RecordSession(RecordSessionMetadata Metadata, Dictionary<string, Stream> WaveData);
