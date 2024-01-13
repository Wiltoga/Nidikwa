using Nidikwa.Common;
using Nidikwa.GUI.ViewModels;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nidikwa.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll([
                ViewModel.ConnectAsync(),
                ViewModel.StartQueueWatcherAsync(),
            ]);
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StartStopRecordAsync();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AddQueueAsync();
        }

        private void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var record = (sender as FrameworkElement)?.DataContext as RecordSessionFile;
            if (record is null)
                return;
            File.Delete(record.File);
        }

        private void OpenEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var record = (sender as FrameworkElement)?.DataContext as RecordSessionFile;
            if (record is null)
                return;
            new RecordEditionWindow(record).ShowDialog();
        }
    }
}