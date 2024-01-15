using Newtonsoft.Json;

namespace Nidikwa.Common;

public enum ResultCodes
{
    Success,
    InvalidEndpoint,
    InvalidInputStructure,
    NotFound,
    InvalidState,
    Timeout,
    NoResponse,
    NotConnected,
    Disconnected,
}

public class Result
{
    public ResultCodes Code { get; init; }
    public string? ErrorMessage { get; init; }
}

public class Result<T> : Result
{
    public T? Data { get; init; }
}

public class ContentResult : Result
{
    public void SetContent(Stream content, int length)
    {
        AdditionnalContent = new LimitedStream(content, length);
        ContentLength = length;
    }
    public int ContentLength { get; set; }
    [JsonIgnore]
    public Stream? AdditionnalContent { get; private set; }
}

internal class LimitedStream : Stream
{
    public LimitedStream(Stream internalStream, int maxLength)
    {
        InternalStream = internalStream;
        this.maxLength = maxLength;
    }

    public Stream InternalStream { get; }
    private int maxLength;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        count = Math.Min(count, maxLength);
        if (count == 0)
            return 0;
        var actualRead = InternalStream.Read(buffer, offset, count);
        maxLength -= actualRead;
        return actualRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}