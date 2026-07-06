using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Client.Interfaces;
using Common;

// ReSharper disable AsyncVoidEventHandlerMethod

namespace Client.Dialogs;

public partial class AddLocalSaveDialog : Window
{
    private readonly IFileSystemPickerService _fileSystemPickerService;
    
    public record Result(bool Valid, string? SavePath, string? SaveName, SaveType SaveType, string? FileExtension);
    
    public AddLocalSaveDialog()
    {
        InitializeComponent();

        DataContext = this;

        _fileSystemPickerService = null!;
    }
    
    public AddLocalSaveDialog(IFileSystemPickerService fileSystemPickerService)
    {
        InitializeComponent();

        DataContext = this;
        
        _fileSystemPickerService = fileSystemPickerService;
    }

    private async void BrowseDirectoryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string? folderPath = await _fileSystemPickerService.PickFolderAsync("Select local save folder");
        if (folderPath is null)
            return;
        DirectoryPathTextBox.Text = folderPath;
        SaveNameTextBox.Text = Path.GetFileName(folderPath);
    }

    private async void BrowseFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string? filePath = await _fileSystemPickerService.OpenFileAsync("Select local save file");
        if (filePath is null)
            return;
        FilePathTextBox.Text = filePath;
        SaveNameTextBox.Text = Path.GetFileNameWithoutExtension(filePath);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SaveType saveType = Common.SaveType.Directory;
        if (SaveTypeFile.IsSelected)
            saveType = Common.SaveType.File;

        string? path = saveType switch
        {
            Common.SaveType.Directory => DirectoryPathTextBox.Text,
            Common.SaveType.File => FilePathTextBox.Text,
            _ => throw new ArgumentOutOfRangeException()
        };

        bool hasSaveName = !string.IsNullOrWhiteSpace(SaveNameTextBox.Text);
        bool hasPath = !string.IsNullOrWhiteSpace(path) && ExistsOnDisk(path, saveType);

        if (!hasPath || !hasSaveName)
        {
            ErrorTextBlock.IsVisible = true;
            ErrorTextBlock.Text = hasSaveName switch
            {
                false when !hasPath => "Please enter a valid save name and path.",
                false when hasPath => "Please enter a valid save name.",
                true when !hasPath => "Please enter a valid save path.",
                _ => ErrorTextBlock.Text
            };
            return;
        }

        ErrorTextBlock.IsVisible = false;
        
        Close(new Result(true, path, SaveNameTextBox.Text, saveType, Path.GetExtension(path)));
    }

    private static bool ExistsOnDisk(string path, SaveType type)
    {
        return type switch
        {
            Common.SaveType.Directory => Directory.Exists(path),
            Common.SaveType.File => File.Exists(path),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}