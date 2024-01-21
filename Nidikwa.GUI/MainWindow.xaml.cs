using Nidikwa.Common;
using Nidikwa.GUI.Pages;
using Nidikwa.GUI.ViewModels;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;

namespace Nidikwa.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public static MainViewModel ViewModel { get; private set; } = null!;

    public MainWindow()
    {
        DataContextChanged += (sender, e) =>
        {
            ViewModel = (e.NewValue as MainViewModel)!;
            ViewModel.Notifications
                .Subscribe(notification =>
                {
                    Snackbar bar = new(snackbarPresenter)
                    {
                        Title = notification.Title,
                        Content = notification.Text,
                        Appearance = notification.Type,
                        Timeout = TimeSpan.FromSeconds(10),
                    };
                    bar.Show();
                })
                .DestroyWith(ViewModel);
        };
        InitializeComponent();
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
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
        RootNavigation.Navigate(typeof(DashboardPage));
        await Task.WhenAll([
            ViewModel.ConnectAsync(),
            ViewModel.StartQueueWatcherAsync(),
        ]);
    }
}