using NAudio.Wave;

namespace Nidikwa.Service;

public class WaveFileStream : Stream
{
    private readonly FileStream cache;
    private readonly Stream writer;
    private bool initialized;

    public WaveFileStream(WaveStream source)
    {
        Source = source;
        cache = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite);
        writer = new WaveFileWriter(cache, Source.WaveFormat);
    }

    private void InitOperation()
    {
        if (initialized)
        {
            return;
        }
        initialized = true;
        Source.CopyTo(writer);
        writer.Flush();
        cache.Seek(0, SeekOrigin.Begin);
    }

    private async Task InitOperationAsync()
    {
        if (initialized)
        {
            return;
        }
        initialized = true;
        await Source.CopyToAsync(writer);
        await writer.FlushAsync();
        cache.Seek(0, SeekOrigin.Begin);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            var filename = cache.Name;
            cache.Dispose();
            File.Delete(filename);
        }
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => cache.Length;

    public override long Position { get => cache.Position; set => cache.Position = value; }

    public WaveStream Source { get; }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        InitOperation();

        return cache.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        InitOperation();

        return cache.Read(buffer);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await InitOperationAsync();

        return await cache.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await InitOperationAsync();

        return await cache.ReadAsync(buffer, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
