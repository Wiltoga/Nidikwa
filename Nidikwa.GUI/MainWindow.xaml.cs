using Nidikwa.GUI.ViewModels;
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
            await ViewModel.ConnectAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StartStopRecordAsync();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await ViewModel.AddQueueAsync();
        }
    }
}