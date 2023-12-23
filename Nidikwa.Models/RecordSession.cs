namespace Nidikwa.Models;

public record RecordSession(RecordSessionMetadata Metadata, ReadOnlyMemory<DeviceSession> DeviceSessions);
