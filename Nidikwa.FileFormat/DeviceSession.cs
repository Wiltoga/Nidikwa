namespace Nidikwa.FileFormat;

public record DeviceSession(string DeviceId, string DeviceName, DeviceType Type, byte[] WaveData);