using Nidikwa.Common;
using Nidikwa.Sdk;
using System.Windows;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for RecordEditionWindow.xaml
    /// </summary>
    public partial class RecordEditionWindow : Window
    {
        public Editor Editor { get; private set; } = default!;
        public RecordSessionFile Session { get; }

        public RecordEditionWindow(RecordSessionFile session)
        {
            Session = session;
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Editor.Dispose();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var defaultDevice = await DevicesAccessor.GetDefaultOutputDeviceAsync();
            Editor = await Editor.CreateAsync(Session, defaultDevice.Id);
        }
    }
}
