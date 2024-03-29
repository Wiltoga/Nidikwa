using Nidikwa.Models;

namespace Nidikwa.FileEncoding.Tests;

[TestClass]
public class SessionIOv1Should
{
    private const ushort version = 1;
    private SessionEncoder encoder;
    private byte[] mockEncodedData;
    private RecordSessionMetadata mockMetadata;
    private RecordSession mockSession;

    [TestMethod]
    public async Task CorrectlyDecodeMetadata()
    {
        var stream = new MemoryStream(mockEncodedData);
        stream.Seek(0, SeekOrigin.Begin);
        var result = await encoder.ParseMetadataAsync(stream, version);

        Assert.AreEqual(mockMetadata.Id, result.Id);
        Assert.AreEqual(mockMetadata.Date, result.Date);
        Assert.AreEqual(mockMetadata.TotalDuration, result.TotalDuration);
    }

    [TestMethod]
    public async Task CorrectlyDecodeSessionData()
    {
        var stream = new MemoryStream(mockEncodedData);
        stream.Seek(0, SeekOrigin.Begin);
        var result = await encoder.ParseSessionAsync(stream, version);

        Assert.AreEqual(mockSession.DeviceSessions.Length, result.DeviceSessions.Length);

        for (int i = 0; i < mockSession.DeviceSessions.Length; ++i)
        {
            Assert.AreEqual(mockSession.DeviceSessions.Span[i].Device.Id, result.DeviceSessions.Span[i].Device.Id);
            Assert.AreEqual(mockSession.DeviceSessions.Span[i].Device.Name, result.DeviceSessions.Span[i].Device.Name);
            Assert.AreEqual(mockSession.DeviceSessions.Span[i].Device.Type, result.DeviceSessions.Span[i].Device.Type);
            Assert.IsTrue(mockSession.DeviceSessions.Span[i].WaveData.Span.SequenceEqual(result.DeviceSessions.Span[i].WaveData.Span));
        }
    }

    [TestMethod]
    public async Task CorrectlyCalculateStreamedLength()
    {
        List<string> tmpFiles = new();
        try
        {
            var fileSession = new RecordSessionAsFile(mockSession.Metadata, mockSession.DeviceSessions.ToArray().Select(session =>
            {
                string temp = Path.GetTempFileName();
                File.WriteAllBytes(temp, session.WaveData.ToArray());
                tmpFiles.Add(temp);
                return new DeviceSessionAsFile(session.Device, temp);
            }).ToArray());

            var computedSize = await encoder.GetStreamedSizeAsync(fileSession, version);
            MemoryStream stream = new();
            await encoder.StreamSessionAsync(fileSession, stream, true, version);
            Assert.AreEqual(computedSize, stream.Length);
        }
        finally
        {
            foreach (var file in tmpFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }

    [TestMethod]
    public async Task CorrectlyEncodeSession()
    {
        var stream = new MemoryStream();
        await encoder.WriteSessionAsync(mockSession, stream, version);

        Assert.IsTrue(mockEncodedData.SequenceEqual(stream.ToArray()));
    }

    [TestInitialize]
    public void Initialize()
    {
        encoder = new SessionEncoder();
        mockSession = new RecordSession(
            mockMetadata = new RecordSessionMetadata(
                new Guid("a7e941ac-e0a6-45fd-9ec5-8ccc6cebbc04"),
                new DateTimeOffset(2000, 01, 01, 12, 10, 5, 156, TimeSpan.Zero),
                TimeSpan.FromTicks(165489568)
            ),
            new[]
            {
                new DeviceSession(
                    new Device(
                        "deviceId1",
                        "device name",
                        DeviceType.Input
                    ),
                    Convert.FromBase64String("fyxhYFp2HiB+GB/MtWzHqzh+3rMgDiw=")
                ),
                new DeviceSession(
                    new Device(
                        "deviceId2",
                        "device other name",
                        DeviceType.Output
                    ),
                    Convert.FromBase64String("fh92B3kIX2Fgf2HfhwhgeA4=")
                ),
            }
        );
        mockEncodedData = Convert.FromBase64String("TkRLVwEArEHpp6bg/UWexYzMbOu8BOQVbG3cAAAAoCvdCQAAAAACAAAACQAAAGRldmljZUlkMQsAAABkZXZpY2UgbmFtZQAXAAAACQAAAGRldmljZUlkMhEAAABkZXZpY2Ugb3RoZXIgbmFtZQERAAAAfyxhYFp2HiB+GB/MtWzHqzh+3rMgDix+H3YHeQhfYWB/Yd+HCGB4Dg==");
    }
}