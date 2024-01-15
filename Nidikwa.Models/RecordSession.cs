namespace Nidikwa.Models;

public record RecordSession(RecordSessionMetadata Metadata, ReadOnlyMemory<DeviceSession> DeviceSessions);

public record RecordSessionAsFile(RecordSessionMetadata Metadata, ReadOnlyMemory<DeviceSessionAsFile> DeviceSessions);
