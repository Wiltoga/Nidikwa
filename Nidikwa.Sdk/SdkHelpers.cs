using NAudio.Wave;

namespace Nidikwa.Sdk;

internal static class SdkHelpers
{
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
