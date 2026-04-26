using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Client.Dialogs;

public partial class ModalDialog : Window
{
    private bool _exitingDueToButtonClick;
    
    public ModalDialog()
    {
        InitializeComponent();
        
        DataContext = this;
    }
    
    public ModalDialog(string title, string message, string yes, string? no)
    {
        InitializeComponent();
        
        DataContext = this;
        
        Title = title;
        Message.Text = message;
        
        YesButton.Content = yes;
        NoButton.Content = no;
        NoButton.IsVisible = !string.IsNullOrWhiteSpace(no);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_exitingDueToButtonClick)
            return;
        e.Cancel = true;
    }

    private void YesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _exitingDueToButtonClick = true;
        Close(true);
    }

    private void NoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _exitingDueToButtonClick = true;
        Close(false);
    }
}