using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Client.Interfaces;

// ReSharper disable AsyncVoidEventHandlerMethod

namespace Client.Dialogs;

public partial class AddLocalSaveDialog : Window
{
    private readonly IFolderPickerService _folderPickerService;
    
    public record Result(bool Valid, string? SavePath, string? SaveName);
    
    public AddLocalSaveDialog()
    {
        InitializeComponent();

        DataContext = this;

        _folderPickerService = null!;
    }
    
    public AddLocalSaveDialog(IFolderPickerService folderPickerService)
    {
        InitializeComponent();

        DataContext = this;
        
        _folderPickerService = folderPickerService;
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string? folderPath = await _folderPickerService.PickFolderAsync("Select local save");
        if (folderPath is null)
            return;
        PathTextBox.Text = folderPath;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        bool hasSaveName = !string.IsNullOrWhiteSpace(SaveNameTextBox.Text);
        bool hasPath = !string.IsNullOrWhiteSpace(PathTextBox.Text) && Directory.Exists(PathTextBox.Text);

        if (!hasPath || !hasSaveName)
        {
            ErrorTextBlock.IsVisible = true;
            if (!hasSaveName && !hasPath)
                ErrorTextBlock.Text = "Please enter a valid save name and path.";
            else if (!hasSaveName)
                ErrorTextBlock.Text = "Please enter a valid save name.";
            else if (!hasPath)
                ErrorTextBlock.Text = "Please enter a valid save path.";
            return;
        }

        ErrorTextBlock.IsVisible = false;

        Close(new Result(true, PathTextBox.Text, SaveNameTextBox.Text));
    }
}