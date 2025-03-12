namespace Nidikwa.Models;

public record RecordSessionMetadata(DateTimeOffset Date, TimeSpan TotalDuration, Device[] Devices);
