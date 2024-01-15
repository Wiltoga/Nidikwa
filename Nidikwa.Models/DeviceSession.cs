namespace Nidikwa.Models;

public record DeviceSession(Device Device, ReadOnlyMemory<byte> WaveData);

public record DeviceSessionAsFile(Device Device, string TempFile);
