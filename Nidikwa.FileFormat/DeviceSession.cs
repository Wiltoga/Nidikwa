namespace Nidikwa.FileFormat;

public record DeviceSession(string DeviceId, string DeviceName, DeviceType Type, ReadOnlyMemory<byte> WaveData);