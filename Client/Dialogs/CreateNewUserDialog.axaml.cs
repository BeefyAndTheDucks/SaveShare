using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Client.Dialogs;

public partial class CreateNewUserDialog : Window
{
    public record Result(bool Valid, string? UserName);
    
    public CreateNewUserDialog()
    {
        InitializeComponent();
        
        DataContext = this;
        
        UserNameTextBox.Text = Environment.MachineName;
    }
    
    public CreateNewUserDialog(string? error)
    {
        InitializeComponent();
        
        DataContext = this;
        
        UserNameTextBox.Text = Environment.MachineName;

        if (error is not null)
        {
            ErrorTextBlock.IsVisible = true;
            ErrorTextBlock.Text = error;
        }
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        bool hasUsername = !string.IsNullOrWhiteSpace(UserNameTextBox.Text) && UserNameTextBox.Text.Length > 2;

        if (!hasUsername)
        {
            ErrorTextBlock.IsVisible = true;
            ErrorTextBlock.Text = "Please enter a valid username.";
            return;
        }

        ErrorTextBlock.IsVisible = false;

        Close(new Result(true, UserNameTextBox.Text));
    }
}