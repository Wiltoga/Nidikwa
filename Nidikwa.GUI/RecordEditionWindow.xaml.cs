using Nidikwa.Common;
using Nidikwa.GUI.ViewModels;
using Nidikwa.Sdk;
using System.Windows;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for RecordEditionWindow.xaml
    /// </summary>
    public partial class RecordEditionWindow : Window
    {
        public RecordSessionFile Session { get; }
        internal EditorViewModel ViewModel { get; private set; } = default!;

        public RecordEditionWindow(RecordSessionFile session)
        {
            Session = session;
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ViewModel.Destroy();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var defaultDevice = await DevicesAccessor.GetDefaultOutputDeviceAsync();
            ViewModel = new EditorViewModel(await Editor.CreateAsync(Session, defaultDevice.Id));
        }
    }
}
