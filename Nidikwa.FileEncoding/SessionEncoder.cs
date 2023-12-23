using Nidikwa.Models;
using System.Text;

namespace Nidikwa.FileEncoding;

public class SessionEncoder
{
    private const string fileType = "NDKW";
    private static ISessionIO[] SessionIOs;

    static SessionEncoder()
    {
        SessionIOs = (from type in typeof(SessionEncoder).Assembly.GetTypes()
                      where type.GetInterfaces().Contains(typeof(ISessionIO)) && type.GetConstructor(Array.Empty<Type>()) is not null
                      select type.GetConstructor(Array.Empty<Type>())!.Invoke(null) as ISessionIO)
            .ToArray();
    }

    public async Task<RecordSessionMetadata> ParseMetadataAsync(Stream stream, ushort? desiredVersion = null, CancellationToken cancellationToken = default)
    {
        var signatureBytes = new byte[4];
        await stream.ReadAsync(signatureBytes);
        if (Encoding.ASCII.GetString(signatureBytes) != fileType)
            throw new FormatException($"The provided file is not a valid {fileType} file");
        var versionBytes = new byte[sizeof(ushort)];
        await stream.ReadAsync(versionBytes);
        var version = BitConverter.ToUInt16(versionBytes);
        if (desiredVersion is not null && version != desiredVersion)
            throw new ArgumentException("The provided file is not suitable for this version");

        var validReader = SessionIOs.FirstOrDefault(reader => reader.FileVersion == version);

        if (validReader is null)
            throw new FormatException("The provided file is encoded in an unknown version");

        return await validReader.ReadMetadataAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecordSession> ParseSessionAsync(Stream stream, ushort? desiredVersion = null, CancellationToken cancellationToken = default)
    {
        var signatureBytes = new byte[4];
        await stream.ReadAsync(signatureBytes);
        if (Encoding.ASCII.GetString(signatureBytes) != fileType)
            throw new FormatException($"The provided file is not a valid {fileType} file");
        var versionBytes = new byte[sizeof(ushort)];
        await stream.ReadAsync(versionBytes);
        var version = BitConverter.ToUInt16(versionBytes);
        if (desiredVersion is not null && version != desiredVersion)
            throw new ArgumentException("The provided file is not suitable for this version");

        var validReader = SessionIOs.FirstOrDefault(reader => reader.FileVersion == version);

        if (validReader is null)
            throw new FormatException("The provided file is encoded in an unknown version");

        return await validReader.ReadSessionAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteSessionAsync(RecordSession session, Stream stream, ushort? version = null, CancellationToken cancellationToken = default)
    {
        ISessionIO? sessionIO = null;
        if (version is null)
        {
            sessionIO = SessionIOs.Aggregate((acc, current) =>
            {
                return current.FileVersion > acc.FileVersion
                    ? current
                    : acc;
            });
        }
        else
        {
            sessionIO = SessionIOs.FirstOrDefault(sessionIO => sessionIO.FileVersion == version);
        }

        if (sessionIO is null)
            throw new ArgumentException("Version not supported");

        var signatureBytes = Encoding.ASCII.GetBytes(fileType);
        var versionBytes = BitConverter.GetBytes(sessionIO.FileVersion);
        await stream.WriteAsync(signatureBytes.Concat(versionBytes).ToArray(), cancellationToken).ConfigureAwait(false);
        await sessionIO.WriteSessionAsync(session, stream, cancellationToken).ConfigureAwait(false);
    }
}