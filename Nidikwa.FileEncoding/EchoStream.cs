using CommunityToolkit.HighPerformance;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Nidikwa.FileEncoding;

/// <summary>
/// https://stackoverflow.com/a/53626801
/// </summary>
public class EchoStream : Stream
{
    // Default underlying mechanism for BlockingCollection is ConcurrentQueue<T>, which is what we want
    private readonly BlockingCollection<ReadOnlyMemory<byte>> _Buffers;

    private readonly object _lock = new object();
    private long _Length = 0L;
    private int _maxQueueDepth = 10;
    private long _Position = 0L;
    private ReadOnlyMemory<byte> m_buffer;
    private bool m_Closed = false;
    private int m_count = 0;
    private bool m_FinalZero = false;
    private int m_offset = 0;
    public override bool CanRead { get; } = true;
    public override bool CanSeek { get; } = false;
    public override bool CanTimeout { get; } = true;
    public override bool CanWrite { get; } = true;
    public bool DataAvailable
    {
        get
        {
            return _Buffers.Count > 0;
        }
    }

    public override long Length
    {
        get
        {
            return _Length;
        }
    }

    public override long Position
    {
        get
        {
            return _Position;
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public override int ReadTimeout { get; set; } = Timeout.Infinite;
    public override int WriteTimeout { get; set; } = Timeout.Infinite;

    public EchoStream() : this(5)
    {
    }

    public EchoStream(int maxQueueDepth)
    {
        _maxQueueDepth = maxQueueDepth;
        _Buffers =  new BlockingCollection<ReadOnlyMemory<byte>>(_maxQueueDepth);
    }

    //after the stream is closed, set to true after returning a 0 for read()
    public override void Close()
    {
        m_Closed = true;

        // release any waiting writes
        _Buffers.CompleteAdding();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan().Slice(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        return Read(buffer, default);
    }

    // we override the xxxxAsync functions because the default base class shares state between ReadAsync and WriteAsync, which causes a hang if both are called at once
    public new Task<int> ReadAsync(byte[] buffer, int offset, int count)
    {
        return Task.Run(() =>
        {
            return Read(buffer, offset, count);
        });
    }

    public override int ReadByte()
    {
        byte[] returnValue = new byte[1];
        return (Read(returnValue, 0, 1) <= 0 ? -1 : (int)returnValue[0]);
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
        Write(buffer.AsSpan().Slice(offset, count));
    }

    public new Task WriteAsync(byte[] buffer, int offset, int count)
    {
        return WriteAsync(buffer, offset, count, default);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Write(buffer, default);
    }

    private void Write(ReadOnlySpan<byte> buffer, CancellationToken token)
    {
        if (m_Closed || buffer.Length <= 0)
            return;

        Memory<byte> newBuffer = new byte[buffer.Length];
        buffer.CopyTo(newBuffer.Span);

        if (!_Buffers.TryAdd(newBuffer, WriteTimeout, token))
            throw new TimeoutException("EchoStream Write() Timeout or Canceled");

        _Length += buffer.Length;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => Write(buffer.Span, cancellationToken), cancellationToken);
    }

    private int Read(Span<byte> buffer, CancellationToken cancellationToken)
    {
        var count = buffer.Length;
        var offset = 0;
        if (count == 0)
            return 0;
        lock (_lock)
        {
            if (m_count == 0 && _Buffers.Count == 0)
            {
                if (m_Closed)
                {
                    if (!m_FinalZero)
                    {
                        m_FinalZero = true;
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }

                if (_Buffers.TryTake(out m_buffer, ReadTimeout, cancellationToken))
                {
                    m_offset = 0;
                    m_count = m_buffer.Length;
                }
                else
                {
                    if (m_Closed)
                    {
                        if (!m_FinalZero)
                        {
                            m_FinalZero = true;
                            return 0;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            int returnBytes = 0;
            while (count > 0)
            {
                if (m_count == 0)
                {
                    if (_Buffers.TryTake(out m_buffer, 0, cancellationToken))
                    {
                        m_offset = 0;
                        m_count = m_buffer.Length;
                    }
                    else
                        break;
                }

                var bytesToCopy = (count < m_count) ? count : m_count;
                m_buffer.Slice(m_offset, bytesToCopy).Span.CopyTo(buffer[offset..]);
                m_offset += bytesToCopy;
                m_count -= bytesToCopy;
                offset += bytesToCopy;
                count -= bytesToCopy;

                returnBytes += bytesToCopy;
            }

            _Position += returnBytes;

            return returnBytes;
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Read(buffer.Span, cancellationToken), cancellationToken);
    }
}