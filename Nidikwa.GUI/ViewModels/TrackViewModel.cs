using Nidikwa.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Nidikwa.GUI.ViewModels;

public class TrackViewModel : ReactiveObject
{
    [Reactive]
    public float[] VisualSamples { get; set; }
    public DeviceSession Session { get; }
    [Reactive]
    public float Volume { get; set; }

    public TrackViewModel(DeviceSession session)
    {
        Session = session;
        Volume = 1;
        VisualSamples = [];
    }
}
