using Nidikwa.Common;
using Nidikwa.GUI.ViewModels;
using System.IO;
using System.Windows;

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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AddQueueAsync();
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StartStopRecordAsync();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll([
                ViewModel.ConnectAsync(),
                ViewModel.StartQueueWatcherAsync(),
            ]);
        }
    }
}