using System.Threading.Tasks;
using Avalonia.Controls;
using Client.Helpers;
using Client.ViewModels;

namespace Client.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    
    public MainWindow(MainWindowViewModel windowViewModel)
    {
        InitializeComponent();
        DataContext = windowViewModel;
        
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;
        
        e.Cancel = viewModel.IsBusy;
        if (viewModel.IsBusy)
            NativeAudio.PlayAlertSound();
    }
}