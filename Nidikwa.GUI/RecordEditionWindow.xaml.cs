using Nidikwa.Common;
using Nidikwa.FileEncoding;
using Nidikwa.GUI.ViewModels;
using Nidikwa.Models;
using Nidikwa.Sdk;
using System.IO;
using System.Windows;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for RecordEditionWindow.xaml
    /// </summary>
    public partial class RecordEditionWindow : Window
    {
        public RecordSessionFile SessionFile { get; }
        internal EditorViewModel ViewModel { get; private set; } = default!;

        public RecordEditionWindow(RecordSessionFile session)
        {
            SessionFile = session;
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
            SessionEncoder encoder = new();
            RecordSession session;
            using (FileStream stream = new(SessionFile.File, FileMode.Open, FileAccess.Read))
            {
                session = await encoder.ParseSessionAsync(stream);
            }
            var defaultDevice = await DevicesAccessor.GetDefaultOutputDeviceAsync();
            DataContext = ViewModel = new EditorViewModel(await Editor.CreateAsync(SessionFile, defaultDevice.Id), session);
        }

        private void NumberTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!float.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }
    }
}
