using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Client.Interfaces;
using Client.ViewModels;
using Common;

// ReSharper disable AsyncVoidEventHandlerMethod

namespace Client.Dialogs;

public partial class AddCloudSaveDialog : Window
{
    public record Result(bool Valid, string? TargetPath, SaveInfo? SaveInfo);
    
    private readonly ISaveCatalogService _saveCatalogService;
    private readonly IFileSystemPickerService _fileSystemPickerService;
    private readonly ITaskRunner _taskRunner;
    private readonly IModalService _modalService;

    public ObservableCollection<CloudSaveInfoViewModel> CloudSaves { get; } = [];

    public AddCloudSaveDialog()
    {
        InitializeComponent();
        
        DataContext = this;

        _saveCatalogService = null!;
        _fileSystemPickerService = null!;
        _taskRunner = null!;
        _modalService = null!;
    }
    
    public AddCloudSaveDialog(ISaveCatalogService saveCatalogService, IFileSystemPickerService fileSystemPickerService, ITaskRunner taskRunner, IModalService modalService)
    {
        InitializeComponent();
        
        DataContext = this;
        
        _saveCatalogService = saveCatalogService;
        _fileSystemPickerService = fileSystemPickerService;
        _taskRunner = taskRunner;
        _modalService = modalService;

        Opened += async (_, _) => await LoadSaves();
    }

    private async Task LoadSaves()
    {
        await _taskRunner.RunAsync(ct => _saveCatalogService.RefreshAsync(ct));

        LocalSaveInfo[] localSaves = _saveCatalogService.LocalSaves;
        
        CloudSaves.Clear();

        foreach (SaveInfo save in _saveCatalogService.CloudSaves)
        {
            bool existsLocally = localSaves.Any(localSave => localSave.SaveId == save.SaveId);
            bool isCheckedOut = !string.IsNullOrEmpty(save.CheckedOutByUserName);
            
            bool canDownload = !existsLocally && !isCheckedOut;

            string? cannotDownloadReason = null;
            
            if (existsLocally)
                cannotDownloadReason = "Already downloaded.";
            else if (isCheckedOut)
                cannotDownloadReason = $"Checked out by {save.CheckedOutByUserName}.";
            
            CloudSaves.Add(new CloudSaveInfoViewModel
            {
                Name = save.Name, NativeObject = save, IsAvailableForDownload = canDownload,
                IsNotAvailableForDownloadReason = cannotDownloadReason
            });
        }
    }

    private CloudSaveInfoViewModel? GetSelectedCloudSaveInfo()
    {
        return CloudSavesList.SelectedItem as CloudSaveInfoViewModel;
    }

    private Result<string> GetDownloadPath(string basePath)
    {
        CloudSaveInfoViewModel? cloudSaveInfoViewModel = GetSelectedCloudSaveInfo();
        if (cloudSaveInfoViewModel is null)
            return Result<string>.Failure("No save selected.");

        if (!Directory.Exists(basePath))
            return Result<string>.Failure("Invalid destination directory.");

        if (Directory.GetFileSystemEntries(basePath).Length == 0) return basePath;
        
        string path = Path.Combine(basePath, cloudSaveInfoViewModel.Name);

        if (!Directory.Exists(path) || Directory.GetFileSystemEntries(path).Length == 0) return path;
        return Result<string>.Failure("Destination directory already exists and isn't empty.");
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SelectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        CloudSaveInfoViewModel? save = GetSelectedCloudSaveInfo();

        if (save is null)
            return;

        string? downloadPath = save.NativeObject.SaveType switch
        {
            SaveType.Directory => await _fileSystemPickerService.PickFolderAsync("Select save folder location"),
            SaveType.File => await _fileSystemPickerService.SaveFileAsync("Select save file location", save.NativeObject.Name, save.NativeObject.FileExtension),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        if (downloadPath is null)
            return;

        var adjustedDownloadPath = GetDownloadPath(downloadPath);

        if (!adjustedDownloadPath.Succeeded)
        {
            await _modalService.ShowAsync("Error", adjustedDownloadPath.Error);
            return;
        }
        
        bool proceed = await _modalService.ShowAsync("Save here?", $"""Save "{save.Name}" at: "{adjustedDownloadPath.Value}"?""", "Yes", "No");
        if (!proceed)
            return;
        
        Close(new Result(true, adjustedDownloadPath.Value, save.NativeObject));
    }

    private void CloudSavesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = CloudSavesList.SelectedItem != null;
    }
}