using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Client.Dialogs;

public partial class NoConnectionDialog : Window
{
    public NoConnectionDialog()
    {
        InitializeComponent();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}