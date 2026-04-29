using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Client.Dialogs;

public partial class InitialSetupWindow : Window
{
    public record Result(bool Valid, Uri? ServerUri);
    
    public InitialSetupWindow()
    {
        InitializeComponent();
        
        DataContext = this;
    }
    
    public InitialSetupWindow(string? error = null)
    {
        InitializeComponent();
        
        DataContext = this;

        ErrorTextBlock.IsVisible = error is not null;
        ErrorTextBlock.Text = error;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ConnectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        bool hasUsername = !string.IsNullOrWhiteSpace(ServerUriTextBox.Text);

        if (!hasUsername)
        {
            ErrorTextBlock.IsVisible = true;
            ErrorTextBlock.Text = "Please enter a valid server URI.";
            return;
        }

        ErrorTextBlock.IsVisible = false;
        
        Close(new Result(true, new Uri(ServerUriTextBox.Text!)));
    }
}