using Nidikwa.Models;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nidikwa.FileEncoding;

public class SessionEncoder
{
    private sealed class TempFileDisposer : IDisposable
    {
        private readonly IEnumerable<FileStream> _files;

        public TempFileDisposer(IEnumerable<FileStream> files)
        {
            _files = files;
        }

        public void Dispose()
        {
            foreach (var file in _files)
            {
                var filename = file.Name;
                file.Dispose();
                File.Delete(filename);
            }
        }
    }

    private const string RecordingFolder = "recordings";
    private const string MetadataFile = "metadata.json";
    private const string RecordingFileExtension = ".wav";
    public async Task WriteAsync(Stream stream, RecordSession recordSession, CancellationToken cancellationToken = default)
    {
        using ZipArchive archive = new(stream, ZipArchiveMode.Create, true);

        using (var metadataStream = archive.CreateEntry(MetadataFile).Open())
        {
            await JsonSerializer.SerializeAsync(metadataStream, recordSession.Metadata, typeof(RecordSessionMetadata), new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter<DeviceType>() }
            }, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var device in recordSession.WaveData)
        {
            using (var wavStream = archive.CreateEntry(Path.Combine(RecordingFolder, $"{device.Key}{RecordingFileExtension}")).Open())
            {
                await device.Value.CopyToAsync(wavStream, cancellationToken);
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task<RecordSessionMetadata> ReadMetadataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, true);

        return await ReadMetadataFromArchiveAsync(archive, cancellationToken);
    }

    private async Task<RecordSessionMetadata> ReadMetadataFromArchiveAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var metadataEntry = archive.GetEntry(MetadataFile);
        if (metadataEntry is null)
        {
            throw new NdkwFileFormatException();
        }
        RecordSessionMetadata? metadata;
        using (var metadataStream = metadataEntry.Open())
        {
            metadata = await JsonSerializer.DeserializeAsync<RecordSessionMetadata>(metadataStream, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter<DeviceType>() }
            }, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (metadata is null)
        {
            throw new NdkwFileFormatException();
        }
        return metadata;
    }

    private async Task<Dictionary<string, FileStream>> ReadSessionFromArchiveAsync(ZipArchive archive, IEnumerable<string> deviceIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, FileStream>();
        foreach (var deviceId in deviceIds)
        {
            var wavEntry = archive.GetEntry(Path.Combine(RecordingFolder, $"{deviceId}{RecordingFileExtension}"));
            if (wavEntry is null)
            {
                throw new NdkwFileFormatException();
            }
            var tempFileStream = new FileStream(Path.GetTempFileName(), FileMode.Open);
            await wavEntry.Open().CopyToAsync(tempFileStream);
            result[deviceId] = tempFileStream;
        }
        return result;
    }

    public async Task<(RecordSession Session, IDisposable StreamsDisposer)> ReadSessionAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, true);

        var metadata = await ReadMetadataFromArchiveAsync(archive, cancellationToken);

        var waveData = await ReadSessionFromArchiveAsync(archive, metadata.Devices.Select(d => d.Id), cancellationToken);

        return (new RecordSession(metadata, waveData.ToDictionary(data => data.Key, data => data.Value as Stream)), new TempFileDisposer(waveData.Values));
    }
}
