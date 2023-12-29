using NAudio.Wave;

namespace Nidikwa.Sdk;

internal class TimeScopeWaveProvider : IWaveProvider
{
    public TimeScopeWaveProvider(IWaveProvider source)
    {
        Source = source;
        ReadBytes = 0;
        Start = TimeSpan.Zero;
        End = TimeSpan.Zero;
    }
    public WaveFormat WaveFormat => Source.WaveFormat;
    public TimeSpan Start { get; set; }
    private int StartOffset => WaveFormat.ConvertLatencyToByteSize((int)Start.TotalMilliseconds);
    private int EndOffset => WaveFormat.ConvertLatencyToByteSize((int)End.TotalMilliseconds);
    public TimeSpan End { get; set; }
    private int ReadBytes { get; set; }
    private IWaveProvider Source { get; }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (ReadBytes < StartOffset)
        {
            var delta = StartOffset - ReadBytes;
            Source.Read(new byte[delta], 0, delta);
            ReadBytes += delta;
        }
        if (ReadBytes + count > EndOffset)
        {
            count = EndOffset - ReadBytes;
        }
        if (count < 0)
            return 0;
        count = Source.Read(buffer, offset, count);
        ReadBytes += count;
        return count;
    }

    public void Reset(TimeSpan offset)
    {
        ReadBytes = WaveFormat.ConvertLatencyToByteSize((int)offset.TotalMilliseconds);
    }
}
