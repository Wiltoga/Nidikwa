using Nidikwa.FileFormat;

namespace Nidikwa.FileEncoding.Tests
{
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
                Assert.AreEqual(mockSession.DeviceSessions[i].DeviceId, result.DeviceSessions[i].DeviceId);
                Assert.AreEqual(mockSession.DeviceSessions[i].DeviceName, result.DeviceSessions[i].DeviceName);
                Assert.AreEqual(mockSession.DeviceSessions[i].Type, result.DeviceSessions[i].Type);
                Assert.IsTrue(mockSession.DeviceSessions[i].WaveData.SequenceEqual(result.DeviceSessions[i].WaveData));
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
                        "deviceId1",
                        "device name",
                        DeviceType.Input,
                        Convert.FromBase64String("fyxhYFp2HiB+GB/MtWzHqzh+3rMgDiw=")
                    ),
                    new DeviceSession(
                        "deviceId2",
                        "device other name",
                        DeviceType.Output,
                        Convert.FromBase64String("fh92B3kIX2Fgf2HfhwhgeA4=")
                    ),
                }
            );
            mockEncodedData = Convert.FromBase64String("TkRLVwEArEHpp6bg/UWexYzMbOu8BOQVbG3cAAAAoCvdCQAAAAACAAAACQAAAGRldmljZUlkMQsAAABkZXZpY2UgbmFtZQAXAAAAAAAAAAkAAABkZXZpY2VJZDIRAAAAZGV2aWNlIG90aGVyIG5hbWUBEQAAAAAAAAB/LGFgWnYeIH4YH8y1bMerOH7esyAOLH4fdgd5CF9hYH9h34cIYHgO");
        }
    }
}