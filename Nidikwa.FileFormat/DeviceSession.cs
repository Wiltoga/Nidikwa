namespace Nidikwa.FileFormat;

public record DeviceSession(string DeviceId, string DeviceName, byte[] WaveData, DeviceType Type);