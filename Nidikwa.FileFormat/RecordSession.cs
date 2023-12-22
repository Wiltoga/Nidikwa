namespace Nidikwa.FileFormat;

public record RecordSession(Guid Id, DateTimeOffset Date, TimeSpan TotalDuration, DeviceSession[] DeviceSessions);