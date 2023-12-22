namespace Nidikwa.FileFormat;

public record RecordSessionMetadata(Guid Id, DateTimeOffset Date, TimeSpan TotalDuration);