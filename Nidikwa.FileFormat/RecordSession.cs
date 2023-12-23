namespace Nidikwa.FileFormat;

public record RecordSession(RecordSessionMetadata Metadata, ReadOnlyMemory<DeviceSession> DeviceSessions);