using System.Windows.Controls;

namespace Nidikwa.GUI.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        DataContext = MainWindow.ViewModel;
        InitializeComponent();
    }
}
