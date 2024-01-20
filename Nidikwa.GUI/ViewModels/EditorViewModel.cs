using Nidikwa.Models;
using Nidikwa.Sdk;
using ReactiveUI.Fody.Helpers;

namespace Nidikwa.GUI.ViewModels;

public class EditorViewModel : DestroyableReactiveObject
{
    public Editor Editor { get; }
    public RecordSession Session { get; }
    public TrackViewModel[] Tracks { get; } 

    public EditorViewModel(Editor editor, RecordSession session)
    {
        Editor = editor;
        Session = session;
        editor.DestroyWith(this);

        Tracks = session.DeviceSessions.ToArray().Select(deviceSession => new TrackViewModel(deviceSession)).ToArray();
    }
}
