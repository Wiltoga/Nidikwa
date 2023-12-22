using Nidikwa.FileFormat;
using System.Text;

namespace Nidikwa.FileEncoding
{
    internal class SessionIOv1 : ISessionIO
    {
        private static (byte Value, DeviceType Type)[] DeviceTypeMapping = new[]
        {
            ((byte)0, DeviceType.Input),
            ((byte)1, DeviceType.Output),
        };

        public ushort FileVersion => 1;

        public async Task<RecordSessionMetadata> ReadMetadataAsync(Stream stream, CancellationToken cancellationToken)
        {
            var id = new Guid(await ParseAsync(stream, 16, cancellationToken).ConfigureAwait(false));

            var date = DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.ToInt64(await ParseAsync(stream, 8, cancellationToken).ConfigureAwait(false)));

            var totalDuration = TimeSpan.FromTicks(BitConverter.ToInt64(await ParseAsync(stream, 8, cancellationToken).ConfigureAwait(false)));

            return new RecordSessionMetadata(id, date, totalDuration);
        }

        public async Task<RecordSession> ReadSessionAsync(Stream stream, CancellationToken cancellationToken)
        {
            var metadata = await ReadMetadataAsync(stream, cancellationToken).ConfigureAwait(false);

            var deviceSessionsCount = BitConverter.ToInt32(await ParseAsync(stream, 4, cancellationToken).ConfigureAwait(false));

            var devices = new (string Id, string Name, DeviceType Type, long DataLength)[deviceSessionsCount];

            for (int i = 0; i < deviceSessionsCount; ++i)
            {
                var idLength = BitConverter.ToInt32(await ParseAsync(stream, 4, cancellationToken).ConfigureAwait(false));
                var id = Encoding.UTF8.GetString(await ParseAsync(stream, idLength, cancellationToken).ConfigureAwait(false));

                var nameLength = BitConverter.ToInt32(await ParseAsync(stream, 4, cancellationToken).ConfigureAwait(false));
                var name = Encoding.UTF8.GetString(await ParseAsync(stream, nameLength, cancellationToken).ConfigureAwait(false));

                var type = Map((await ParseAsync(stream, 1, cancellationToken).ConfigureAwait(false))[0]);

                var dataLength = BitConverter.ToInt64(await ParseAsync(stream, 8, cancellationToken).ConfigureAwait(false));

                devices[i] = ((id, name, type, dataLength));
            }

            var deviceSessions = new DeviceSession[deviceSessionsCount];

            for (int i = 0; i < deviceSessionsCount; ++i)
            {
                var waveData = await ParseAsync(stream, devices[i].DataLength, cancellationToken).ConfigureAwait(false);

                deviceSessions[i] = new DeviceSession(devices[i].Id, devices[i].Name, devices[i].Type, waveData);
            }

            return new RecordSession(metadata, deviceSessions);
        }

        public async Task WriteSessionAsync(RecordSession recordSession, Stream stream, CancellationToken cancellationToken)
        {
            #region metadata

            await stream.WriteAsync(recordSession.Metadata.Id.ToByteArray(), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            await stream.WriteAsync(BitConverter.GetBytes(recordSession.Metadata.Date.ToUnixTimeMilliseconds()), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            await stream.WriteAsync(BitConverter.GetBytes(recordSession.Metadata.TotalDuration.Ticks), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            #endregion metadata

            #region devices listing

            await stream.WriteAsync(BitConverter.GetBytes(recordSession.DeviceSessions.Length), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var deviceSession in recordSession.DeviceSessions)
            {
                await stream.WriteAsync(BitConverter.GetBytes(deviceSession.DeviceId.Length), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                await stream.WriteAsync(Encoding.UTF8.GetBytes(deviceSession.DeviceId), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                await stream.WriteAsync(BitConverter.GetBytes(deviceSession.DeviceName.Length), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                await stream.WriteAsync(Encoding.UTF8.GetBytes(deviceSession.DeviceName), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                await stream.WriteAsync(new[] { Map(deviceSession.Type) }, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                await stream.WriteAsync(BitConverter.GetBytes(deviceSession.WaveData.LongLength), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            #endregion devices listing

            #region wave data writing

            foreach (var deviceSession in recordSession.DeviceSessions)
            {
                await stream.WriteAsync(deviceSession.WaveData, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            #endregion wave data writing
        }

        private static byte Map(DeviceType deviceType)
        {
            foreach (var mapping in DeviceTypeMapping)
            {
                if (mapping.Type == deviceType)
                    return mapping.Value;
            }
            throw new ArgumentException("Unknown device type");
        }

        private static DeviceType Map(byte mappedDeviceType)
        {
            foreach (var mapping in DeviceTypeMapping)
            {
                if (mapping.Value == mappedDeviceType)
                    return mapping.Type;
            }
            throw new ArgumentException("Unknown device type");
        }

        private static async Task<byte[]> ParseAsync(Stream stream, long bufferLength, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferLength];
            if (await stream.ReadAsync(buffer, cancellationToken) < bufferLength)
                throw new FormatException("The file is corrupted or invalid for this version");
            cancellationToken.ThrowIfCancellationRequested();

            return buffer;
        }
    }
}