using Avalonia.Controls;
using Client.Dialogs;
using Client.ViewModels;

namespace Client.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    
    public MainWindow(MainWindowViewModel windowViewModel)
    {
        InitializeComponent();
        DataContext = windowViewModel;
    }
}