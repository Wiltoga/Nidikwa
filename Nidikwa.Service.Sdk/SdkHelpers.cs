using NAudio.Wave;

namespace Nidikwa.Service.Sdk;

internal static class SdkHelpers
{
    private class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> internalBuffer;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> internalBuffer)
        {
            this.internalBuffer = internalBuffer;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => internalBuffer.Length;

        public int position;
        public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, internalBuffer.Length - position);
            internalBuffer[position..(position + count)].CopyTo(buffer.AsMemory()[offset..]);
            position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            position = origin switch
            {
                SeekOrigin.Begin => (int)offset,
                SeekOrigin.Current => position + (int)offset,
                SeekOrigin.End => internalBuffer.Length + (int)offset,
                _ => throw new NotSupportedException(),
            };
            position = Math.Clamp(position, 0, internalBuffer.Length);
            return position;
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
    public static Stream AsStream(this ReadOnlyMemory<byte> memory) => new ReadOnlyMemoryStream(memory);
    public static Task CopyToAsync(this IWaveProvider provider, Stream stream)
    {
        var chunkSize = provider.WaveFormat.BlockAlign * (1 << 10);
        return Task.Run(() =>
        {
            int readBytes;
            var buffer = new byte[chunkSize];
            while ((readBytes = provider.Read(buffer, 0, chunkSize)) > 0)
            {
                stream.Write(buffer, 0, readBytes);
            }
        });
    }
    public static IEnumerable<(T First, T Second)> AsJoints<T>(this IEnumerable<T> source)
    {
        T lastValue = default!;
        bool firstValue = true;
        foreach (var item in source)
        {
            if (firstValue)
            {
                firstValue = true;
            }
            else
            {
                yield return (lastValue!, item);
            }
            lastValue = item;
        }
    }
}
