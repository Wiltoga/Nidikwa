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
            Source = source;
            Format = format;
            Resampler = new MediaFoundationResampler(new RawSourceWaveStream(source, format), OutputFormat);

            Output = Resampler;
        }

        public Stream Source { get; }
        public WaveFormat Format { get; }
        public IWaveProvider Resampler { get; }
        public IWaveProvider Output { get; }

        public void Dispose()
        {
            Source.Dispose();
        }
    }
    private Editor(string file, RecordSession session, IDictionary<string, DeviceSessionEdition> devices, MMDevice playbackDevice)
    {
        File = file;
        Session = session;
        PlaybackDevice = playbackDevice;
        Player = new WasapiOut(PlaybackDevice, AudioClientShareMode.Shared, true, 200);


        DeviceSessions = devices;
        

        var mixer = new MixingSampleProvider(DeviceSessions.Values.Select(session => session.Output.ToSampleProvider()));
        mixer.ReadFully = false;
        Mixer = mixer.ToWaveProvider();

        FinalResampler = new MediaFoundationResampler(Mixer, playbackDevice.AudioClient.MixFormat);
        Player.Init(FinalResampler);

        Player.PlaybackStopped += Player_PlaybackStopped;
    }

    private void Player_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        IsPlaying = false;
    }

    public string File { get; }
    public RecordSession Session { get; }
    public bool IsPlaying { get; private set; }
    private MMDevice PlaybackDevice { get; }
    private IWavePlayer Player { get; }
    private IDictionary<string, DeviceSessionEdition> DeviceSessions { get; }
    private IWaveProvider Mixer { get; }
    private IWaveProvider FinalResampler { get; }

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
                using var waveData = new MemoryStream(session.WaveData.ToArray());
                using var wavereader = new WaveFileReader(waveData);
                var rawData = new MemoryStream();
                await wavereader.CopyToAsync(rawData);
                return new KeyValuePair<string, DeviceSessionEdition>(session.Device.Id, new DeviceSessionEdition(rawData, wavereader.WaveFormat));
            }));

        return new Editor(sessionFile.File, session, new Dictionary<string, DeviceSessionEdition>(devices), device);
    }

    public void Play()
    {
        if (IsPlaying)
            return;

        IsPlaying = true;
        foreach (var session in DeviceSessions.Values)
        {
            session.Source.Seek(0, SeekOrigin.Begin);
        }

        Player.Play();

    }

    public void Dispose()
    {
        foreach (var session in DeviceSessions.Values)
        {
            session.Dispose();
        }

        Player.PlaybackStopped -= Player_PlaybackStopped;
    }
}
