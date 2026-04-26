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
    private readonly IFolderPickerService _folderPickerService;

    public ObservableCollection<CloudSaveInfoViewModel> CloudSaves { get; } = [];

    public AddCloudSaveDialog()
    {
        InitializeComponent();
        
        DataContext = this;

        _saveCatalogService = null!;
        _folderPickerService = null!;
    }
    
    public AddCloudSaveDialog(ISaveCatalogService saveCatalogService, IFolderPickerService folderPickerService)
    {
        InitializeComponent();
        
        DataContext = this;
        
        _saveCatalogService = saveCatalogService;
        _folderPickerService = folderPickerService;

        Opened += async (_, _) => await LoadSaves();
    }

    private async Task LoadSaves()
    {
        await _saveCatalogService.RefreshAsync();

        LocalSaveEntry[] localSaves = _saveCatalogService.LocalSaves;
        
        CloudSaves.Clear();
        foreach (SaveInfo save in _saveCatalogService.CloudSaves.Where(save => localSaves.All(localSave => localSave.SaveId != save.SaveId)))
            CloudSaves.Add(new CloudSaveInfoViewModel { Name = save.Name, SaveId = save.SaveId });
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string? folderPath = await _folderPickerService.PickFolderAsync("Select download location");
        if (folderPath is null)
            return;
        PathTextBox.Text = folderPath;
    }

    private void PathTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateDownloadTargetInfo();
    }

    private CloudSaveInfoViewModel? GetSelectedCloudSaveInfo()
    {
        return CloudSavesList.SelectedItem as CloudSaveInfoViewModel;
    }

    private Result<string> GetDownloadPath()
    {
        CloudSaveInfoViewModel? cloudSaveInfoViewModel = GetSelectedCloudSaveInfo();
        if (cloudSaveInfoViewModel is null)
            return Result<string>.Failure("No save selected.");

        if (!Directory.Exists(PathTextBox.Text))
            return Result<string>.Failure("Invalid destination directory.");

        if (Directory.GetFileSystemEntries(PathTextBox.Text).Length == 0) return PathTextBox.Text;
        
        string path = Path.Combine(PathTextBox.Text, cloudSaveInfoViewModel.Name);

        if (!Directory.Exists(path) || Directory.GetFileSystemEntries(path).Length == 0) return path;
        return Result<string>.Failure("Destination directory already exists and isn't empty.");
    }

    private void UpdateDownloadTargetInfo()
    {
        Result<string> downloadPath = GetDownloadPath();
        if (!downloadPath.Succeeded)
        {
            DownloadTargetInfo.IsVisible = false;
            return;
        }
        
        DownloadTargetInfo.IsVisible = true;
        DownloadTargetInfo.Text = $"Downloading to {downloadPath.Value}";
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateDownloadTargetInfo();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result<string> path = GetDownloadPath();
        if (!path.Succeeded)
        {
            ErrorTextBlock.Text = path.Error;
            ErrorTextParent.IsVisible = true;
            return;
        }

        ErrorTextParent.IsVisible = false;
        CloudSaveInfoViewModel? cloudSaveInfoViewModel = GetSelectedCloudSaveInfo();
        Close(new Result(true, path.Value, _saveCatalogService.CloudSaves.First(save => save.SaveId == cloudSaveInfoViewModel?.SaveId)));
    }
}