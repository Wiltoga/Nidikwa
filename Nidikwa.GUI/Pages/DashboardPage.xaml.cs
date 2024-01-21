using System.Windows.Controls;

namespace Nidikwa.GUI.Pages;

/// <summary>
/// Interaction logic for DashboardPage.xaml
/// </summary>
public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        DataContext = MainWindow.ViewModel;
        InitializeComponent();
    }
}
