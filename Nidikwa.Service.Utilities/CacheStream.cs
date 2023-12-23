namespace Nidikwa.Service.Utilities;

public class CacheStream : Stream
{
    private long writtenBytes;
    private readonly long maxLength;
    private readonly long cacheReferenceStart;
    private long virtualPosition;
    private long cacheOffset;
    private Stream InternalMemory { get; }
    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => writtenBytes;

    public override long Position { get => virtualPosition; set => Seek(value, SeekOrigin.Begin); }
    public CacheStream(Stream internalMemory, long maxLength)
    {
        InternalMemory = internalMemory;
        cacheReferenceStart = internalMemory.Position;
        virtualPosition = 0;
        writtenBytes = 0;
        cacheOffset = 0;
        this.maxLength = maxLength;
    }

    public void Clear()
    {
        InternalMemory.Seek(cacheReferenceStart, SeekOrigin.Begin);
        virtualPosition = 0;
        writtenBytes = 0;
        cacheOffset = 0;
    }

    public override void Flush()
    {
        InternalMemory.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var readBytes = (int)Math.Min(count, writtenBytes - virtualPosition);
        if (readBytes == 0)
            return 0;

        virtualPosition += readBytes;

        var readInternalBytes = InternalMemory.Read(buffer, offset, readBytes);
        if (readInternalBytes == readBytes)
            return readBytes;

        InternalMemory.Seek(cacheReferenceStart, SeekOrigin.Begin);
        offset += readInternalBytes;
        InternalMemory.Read(buffer, offset, readBytes - readInternalBytes);
        return readBytes;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        virtualPosition = origin switch
        {
            SeekOrigin.Begin => Math.Clamp(offset, 0, writtenBytes),
            SeekOrigin.Current => Math.Clamp(virtualPosition + offset, 0, writtenBytes),
            SeekOrigin.End => Math.Clamp(writtenBytes + offset, 0, writtenBytes),
            _ => throw new NotSupportedException(),
        };
        InternalMemory.Seek(cacheReferenceStart + ((virtualPosition + cacheOffset) % maxLength), SeekOrigin.Begin);

        return virtualPosition;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count > maxLength)
        {
            offset += count - (int)maxLength;
            count = (int)maxLength;
        }

        var scopedWrittenBytes = 0;

        if (InternalMemory.Position - cacheReferenceStart + count > maxLength)
        {
            var maxWrittable = maxLength - InternalMemory.Position + cacheReferenceStart;
            InternalMemory.Write(buffer, offset, (int)maxWrittable);
            scopedWrittenBytes += (int)maxWrittable;
            count -= (int)maxWrittable;
            offset += (int)maxWrittable;
            InternalMemory.Seek(cacheReferenceStart, SeekOrigin.Begin);
        }
        InternalMemory.Write(buffer, offset, count);
        scopedWrittenBytes += count;

        var overflow = virtualPosition + scopedWrittenBytes - maxLength;
        if (overflow > 0)
        {
            cacheOffset = (cacheOffset + overflow) % maxLength;
        }
        writtenBytes = Math.Min(writtenBytes + scopedWrittenBytes, maxLength);
    }
}
