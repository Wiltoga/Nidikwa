using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Nidikwa.FileEncoding;
using Nidikwa.Models;
using Nidikwa.Common;
using CommunityToolkit.HighPerformance;
using NAudio.Utils;

namespace Nidikwa.Sdk;

public class Editor : IDisposable
{
    private static WaveFormat OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    public TimeSpan End { get => Scope.End; set => Scope.End = value; }

    public string File { get; }

    public TimeSpan FullDuration { get; }

    public bool IsPlaying { get; private set; }

    public float MasterVolume { get => Volume.Volume; set => Volume.Volume = value; }

    public RecordSession Session { get; }

    public TimeSpan Start { get => Scope.Start; set => Scope.Start = value; }

    private MediaFoundationResampler DeviceResampler { get; }

    private IDictionary<string, DeviceSessionEdition> DeviceSessions { get; }

    private Dictionary<string, float> HighestSample { get; }

    private MixingWaveProvider32 Mixer { get; }

    private string PlaybackDevice { get; }

    private IWavePlayer? Player { get; set; }

    private CancellationTokenSource? PositionCallbackTokenSource { get; set; }

    private TimeScopeWaveProvider Scope { get; }

    private VolumeSampleProvider Volume { get; }

    private Editor(string file, RecordSession session, IDictionary<string, DeviceSessionEdition> devices, string playbackDevice)
    {
        File = file;
        Session = session;
        PlaybackDevice = playbackDevice;

        DeviceSessions = devices;

        HighestSample = new();

        foreach (var device in devices)
        {
            float max = 0;
            foreach (var sample in device.Value.Samples.Span)
            {
                var abs = Math.Abs(sample);
                if (abs > max)
                    max = abs;
            }
            HighestSample[device.Key] = max;
        }

        Mixer = new MixingWaveProvider32(DeviceSessions.Values.Select(session => session.Output));

        Scope = new TimeScopeWaveProvider(Mixer);

        Volume = new VolumeSampleProvider(Scope.ToSampleProvider());

        DeviceResampler = new MediaFoundationResampler(Volume.ToWaveProvider(), DevicesAccessor.Enumerator.GetDevice(playbackDevice).AudioClient.MixFormat);

        FullDuration = Scope.End = session.Metadata.TotalDuration;
    }

    public static async Task<Editor> CreateAsync(RecordSessionFile sessionFile, string playbackDeviceId, CancellationToken token = default)
    {
        using var stream = new FileStream(sessionFile.File, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new SessionEncoder();

        var session = await reader.ParseSessionAsync(stream, null, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        var devices = await Task.WhenAll(session.DeviceSessions.ToArray().Select(
            async session =>
            {
                using var waveData = session.WaveData.AsStream();
                using var wavereader = new WaveFileReader(waveData);
                using var resampler = new MediaFoundationResampler(wavereader, OutputFormat);
                var rawData = new MemoryStream();
                await resampler.CopyToAsync(rawData).ConfigureAwait(false);
                rawData.Seek(0, SeekOrigin.Begin);
                return new KeyValuePair<string, DeviceSessionEdition>(session.Device.Id, new DeviceSessionEdition(rawData, resampler.WaveFormat));
            })).ConfigureAwait(false);

        return new Editor(sessionFile.File, session, new Dictionary<string, DeviceSessionEdition>(devices), playbackDeviceId);
    }

    public void Dispose()
    {
        foreach (var session in DeviceSessions.Values)
        {
            session.Dispose();
        }

        Player?.Stop();
    }

    public async Task ExportAsync(string file, ExportEncoding encoding)
    {
        if (IsPlaying)
            return;
        MoveReader(TimeSpan.Zero);

        var waveProvider = Volume.ToWaveProvider();

        await Task.Run(() =>
        {
            using var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);
            switch (encoding)
            {
                case ExportEncoding.Wav:
                    WaveFileWriter.WriteWavFileToStream(stream, waveProvider);
                    break;

                case ExportEncoding.Mp3_64k:
                    MediaFoundationEncoder.EncodeToMp3(waveProvider, stream, 64_000);
                    break;

                case ExportEncoding.Mp3_96k:
                    MediaFoundationEncoder.EncodeToMp3(waveProvider, stream, 96_000);
                    break;

                case ExportEncoding.Mp3_128k:
                    MediaFoundationEncoder.EncodeToMp3(waveProvider, stream, 128_000);
                    break;

                case ExportEncoding.Mp3_192k:
                    MediaFoundationEncoder.EncodeToMp3(waveProvider, stream, 192_000);
                    break;

                case ExportEncoding.Aac_64k:
                    MediaFoundationEncoder.EncodeToAac(waveProvider, stream, 64_000);
                    break;

                case ExportEncoding.Aac_96k:
                    MediaFoundationEncoder.EncodeToAac(waveProvider, stream, 96_000);
                    break;

                case ExportEncoding.Aac_128k:
                    MediaFoundationEncoder.EncodeToAac(waveProvider, stream, 128_000);
                    break;

                case ExportEncoding.Aac_192k:
                    MediaFoundationEncoder.EncodeToAac(waveProvider, stream, 192_000);
                    break;

                case ExportEncoding.Wma_64k:
                    MediaFoundationEncoder.EncodeToWma(waveProvider, stream, 64_000);
                    break;

                case ExportEncoding.Wma_96k:
                    MediaFoundationEncoder.EncodeToWma(waveProvider, stream, 96_000);
                    break;

                case ExportEncoding.Wma_128k:
                    MediaFoundationEncoder.EncodeToWma(waveProvider, stream, 128_000);
                    break;

                case ExportEncoding.Wma_192k:
                    MediaFoundationEncoder.EncodeToWma(waveProvider, stream, 192_000);
                    break;
            }
        }).ConfigureAwait(false);
    }

    public Task<float[]> GetAverageSamplesBetweenAsync(TimeSpan start, TimeSpan end, int samplesCount, string sessionId, CancellationToken token)
    {
        var maxScopeTime = TimeSpan.FromSeconds(.1);
        var deviceSession = DeviceSessions[sessionId];
        return Task.Run(() =>
        {
            var startOffset = deviceSession.RawStream.WaveFormat.ConvertLatencyToByteSize((int)start.TotalMilliseconds) / (deviceSession.RawStream.WaveFormat.BitsPerSample / 8);
            var endOffset = deviceSession.RawStream.WaveFormat.ConvertLatencyToByteSize((int)end.TotalMilliseconds) / (deviceSession.RawStream.WaveFormat.BitsPerSample / 8);

            var samplesDuration = endOffset - startOffset;
            if (samplesDuration < samplesCount)
                samplesCount = samplesDuration;

            // distance in sample count between each average sample calculation
            var samplesDelta = (int)Math.Ceiling((float)samplesDuration / samplesCount);
            // max possible range of samples for the average calculation
            var maxScopeSamples = deviceSession.RawStream.WaveFormat.ConvertLatencyToByteSize((int)maxScopeTime.TotalMilliseconds) / (deviceSession.RawStream.WaveFormat.BitsPerSample / 8);

            // actual amount of real samples used to compute one average sample
            var computedScope = Math.Min(samplesDelta, maxScopeSamples);

            var resultSamples = new float[samplesCount];

            var lastSampleIsMin = false;
            for (int i = 0; i < samplesCount; ++i)
            {
                token.ThrowIfCancellationRequested();
                var scope = deviceSession.Samples.Span.Slice(startOffset + (i * samplesDelta), computedScope);
                var max = 0f;
                var min = 0f;
                foreach (var sample in scope)
                {
                    if (sample > max)
                        max = sample;
                    if (sample < min)
                        min = sample;
                }

                if (-min > max * 1.0001f)
                {
                    resultSamples[i] = min;
                    lastSampleIsMin = true;
                }
                else if (-min * 1.0001f < max)
                {
                    resultSamples[i] = max;
                    lastSampleIsMin = false;
                }
                else if (lastSampleIsMin)
                {
                    resultSamples[i] = max;
                    lastSampleIsMin = false;
                }
                else
                {
                    resultSamples[i] = min;
                    lastSampleIsMin = true;
                }
            }

            return resultSamples;
        });
    }

    public float GetSessionHighestSample(string sessionId) => HighestSample[sessionId];

    public float GetSessionVolume(string sessionId) => DeviceSessions[sessionId].Volume.Volume;

    public void MoveReader(TimeSpan offset)
    {
        if (IsPlaying)
            return;

        foreach (var session in DeviceSessions.Values)
        {
            var size = session.RawStream.WaveFormat.ConvertLatencyToByteSize((int)offset.TotalMilliseconds);
            session.RawStream.Seek(size, SeekOrigin.Begin);
        }
        DeviceResampler.Reposition();
        Scope.Reset(offset);
    }

    public void Pause()
    {
        if (Player is null || Player.PlaybackState != PlaybackState.Playing)
            return;

        Player?.Pause();
    }

    public void Play(Action<TimeSpan>? positionCallback = null)
    {
        if (Player is { PlaybackState: PlaybackState.Playing or PlaybackState.Stopped })
            return;

        IsPlaying = true;

        var device = DevicesAccessor.Enumerator.GetDevice(PlaybackDevice);

        var player = new WasapiOut(device, AudioClientShareMode.Shared, true, 200);
        Player = player;
        Player.Init(DeviceResampler);

        Player.PlaybackStopped += Player_PlaybackStopped;

        Player.Play();

        if (positionCallback is not null)
        {
            PositionCallbackTokenSource = new();
            Task.Run(() =>
            {
                while (!PositionCallbackTokenSource.Token.IsCancellationRequested)
                {
                    positionCallback.Invoke(Scope.Start + player.GetPositionTimeSpan());
                    Thread.Sleep(10);
                }
            });
        }
    }

    public float SetSessionVolume(string sessionId, float volume) => DeviceSessions[sessionId].Volume.Volume = volume;

    public void Stop()
    {
        if (Player is null || Player.PlaybackState == PlaybackState.Stopped)
            return;

        Player?.Stop();
        Scope.Reset(TimeSpan.Zero);
        foreach (var session in DeviceSessions.Values)
        {
            session.RawStream.Seek(0, SeekOrigin.Begin);
        }
    }

    private void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PositionCallbackTokenSource?.Cancel();
        PositionCallbackTokenSource = null;
        var player = sender as IWavePlayer;
        if (player is null)
            return;
        IsPlaying = false;
        player.PlaybackStopped -= Player_PlaybackStopped;
        player.Dispose();
        Player = null;
    }

    private class DeviceSessionEdition : IDisposable
    {
        public IWaveProvider Output { get; }

        public WaveStream RawStream { get; }

        public ReadOnlyMemory<float> Samples { get; }

        public VolumeSampleProvider Volume { get; }

        public DeviceSessionEdition(Stream source, WaveFormat format)
        {
            RawStream = new RawSourceWaveStream(source, format);

            var monoFormat = WaveFormat.CreateIeeeFloatWaveFormat(RawStream.WaveFormat.SampleRate, 1);
            var asMonoSamples = new MediaFoundationResampler(RawStream, monoFormat).ToSampleProvider();
            RawStream.Seek(0, SeekOrigin.Begin);
            var samples = new float[RawStream.Length / (RawStream.WaveFormat.BitsPerSample / 8)];
            asMonoSamples.Read(samples, 0, samples.Length);
            Samples = samples;
            RawStream.Seek(0, SeekOrigin.Begin);

            Volume = new VolumeSampleProvider(RawStream.ToSampleProvider());

            Output = Volume.ToWaveProvider();
        }

        public void Dispose()
        {
            RawStream.Dispose();
        }
    }
}
