namespace Nidikwa.Models;

public record DeviceSession(Device Device, ReadOnlyMemory<byte> WaveData);
