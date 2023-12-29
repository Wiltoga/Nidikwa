using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Nidikwa.FileEncoding;
using Nidikwa.Models;
using Nidikwa.Service.Utilities;

namespace Nidikwa.Service.Sdk;

public class Editor : IDisposable
{
    private static WaveFormat OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    private class DeviceSessionEdition : IDisposable
    {
        public DeviceSessionEdition(Stream source, WaveFormat format)
        {
            RawStream = new RawSourceWaveStream(source, format);

            Output = RawStream;
        }

        public WaveStream RawStream { get; }
        public IWaveProvider Output { get; }

        public void Dispose()
        {
            RawStream.Dispose();
        }
    }

    private Editor(string file, RecordSession session, IDictionary<string, DeviceSessionEdition> devices, MMDevice playbackDevice)
    {
        File = file;
        Session = session;
        PlaybackDevice = playbackDevice;

        DeviceSessions = devices;

        Mixer = new MultiplexingWaveProvider(DeviceSessions.Values.Select(session => session.Output));

        Scope = new TimeScopeWaveProvider(Mixer);

        Volume = new VolumeSampleProvider(Scope.ToSampleProvider());

        DeviceResampler = new MediaFoundationResampler(Volume.ToWaveProvider(), playbackDevice.AudioClient.MixFormat);

        FullDuration = Scope.End = session.Metadata.TotalDuration;
    }

    private void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        var player = sender as IWavePlayer;
        if (player is null)
            return;
        IsPlaying = false;
        player.PlaybackStopped -= Player_PlaybackStopped;
        player.Dispose();
        Player = null;
    }

    public string File { get; }
    public RecordSession Session { get; }
    public bool IsPlaying { get; private set; }
    private MMDevice PlaybackDevice { get; }
    private IWavePlayer? Player { get; set; }
    private IDictionary<string, DeviceSessionEdition> DeviceSessions { get; }
    private MultiplexingWaveProvider Mixer { get; }
    private TimeScopeWaveProvider Scope { get; }
    private VolumeSampleProvider Volume { get; }
    private MediaFoundationResampler DeviceResampler { get; }
    public TimeSpan FullDuration { get; }
    public TimeSpan Start { get => Scope.Start; set => Scope.Start = value; }
    public TimeSpan End { get => Scope.End; set => Scope.End = value; }
    public float VolumeLevel { get => Volume.Volume; set => Volume.Volume = value; }

    public static async Task<Editor> CreateAsync(RecordSessionFile sessionFile, string playbackDeviceId, CancellationToken token = default)
    {
        using var stream = new FileStream(sessionFile.File, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new SessionEncoder();

        var session = await reader.ParseSessionAsync(stream, null, token);
        token.ThrowIfCancellationRequested();
        var devicesEnumerator = new MMDeviceEnumerator();
        var device = devicesEnumerator.GetDevice(playbackDeviceId);

        if (device.DataFlow != DataFlow.Render)
            throw new InvalidOperationException("The provided device is not an output device");

        var devices = await Task.WhenAll(session.DeviceSessions.ToArray().Select(
            async session =>
            {
                using var waveData = session.WaveData.AsStream();
                using var wavereader = new WaveFileReader(waveData);
                using var resampler = new MediaFoundationResampler(wavereader, OutputFormat);
                var rawData = new MemoryStream();
                await resampler.CopyToAsync(rawData);
                rawData.Seek(0, SeekOrigin.Begin);
                return new KeyValuePair<string, DeviceSessionEdition>(session.Device.Id, new DeviceSessionEdition(rawData, resampler.WaveFormat));
            }));

        return new Editor(sessionFile.File, session, new Dictionary<string, DeviceSessionEdition>(devices), device);
    }

    public void Play()
    {
        if (Player is { PlaybackState: PlaybackState.Playing or PlaybackState.Stopped })
            return;

        IsPlaying = true;

        Player = new WasapiOut(PlaybackDevice, AudioClientShareMode.Shared, true, 200);
        Player.Init(DeviceResampler);

        Player.PlaybackStopped += Player_PlaybackStopped;

        Player.Play();
    }

    public void Pause()
    {
        if (Player is null || Player.PlaybackState != PlaybackState.Playing)
            return;

        Player?.Pause();
    }

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

    public void Dispose()
    {
        foreach (var session in DeviceSessions.Values)
        {
            session.Dispose();
        }

        Player?.Stop();
    }
}
