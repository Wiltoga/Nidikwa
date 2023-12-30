using Nidikwa.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Nidikwa.GUI.ViewModels;

public class DeviceViewModel : ReactiveObject
{
    public DeviceViewModel(Device reference)
    {
        Reference = reference;
    }
    [Reactive]
    public bool Selected { get; set; }
    public Device Reference { get; }
}
