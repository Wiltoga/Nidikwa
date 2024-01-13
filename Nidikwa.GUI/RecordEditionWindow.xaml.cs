using Nidikwa.Common;
using System.ServiceProcess;
using System.Windows;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for RecordEditionWindow.xaml
    /// </summary>
    public partial class RecordEditionWindow : Window
    {
        public RecordSessionFile Session { get; }

        public RecordEditionWindow(RecordSessionFile session)
        {
            InitializeComponent();
            Session = session;
        }
    }
}
