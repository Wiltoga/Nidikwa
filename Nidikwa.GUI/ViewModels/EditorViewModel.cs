using Nidikwa.Sdk;
using ReactiveUI.Fody.Helpers;

namespace Nidikwa.GUI.ViewModels;

internal class EditorViewModel : DestroyableReactiveObject
{
    public Editor Editor { get; }
    [Reactive]
    public float[] VisualSamples { get; set; }

    public EditorViewModel(Editor editor)
    {
        Editor = editor;
        editor.DestroyWith(this);
    }
}
