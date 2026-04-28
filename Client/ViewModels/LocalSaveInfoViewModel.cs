using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Client.Exceptions;
using Client.Interfaces;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class LocalSaveInfoViewModel(IModalService modalService, ISaveCatalogService saveCatalogService, IMainWindowProvider mainWindowProvider) : ViewModelBase
{
    [ObservableProperty] public partial string Name { get; set; } = "???";
    
    [ObservableProperty] public partial string? InUseBy { get; set; }
    
    [ObservableProperty] public partial string? LastChangedBy { get; set; }
    [ObservableProperty] public partial DateTime? LastChangedAt { get; set; }

    [ObservableProperty] public partial bool ExistsOnServer { get; set; } = true;
    
    [ObservableProperty] public partial string? ActionButtonAction { get; private set; }

    [ObservableProperty] public partial string? CurrentOperation { get; set; }

    [ObservableProperty] public partial double CurrentOperationProgress { get; set; }

    [ObservableProperty] public partial string? CurrentSubOperation { get; set; }

    [ObservableProperty] public partial bool CurrentSubOperationIndeterminate { get; set; }

    [ObservableProperty] public partial double CurrentSubOperationProgress { get; set; }
    
    public SaveId Id { get; set; }
    
    public enum ActionType { Nothing = 0, UploadChanges, DownloadChanges, TakeInUse }

    public ActionType ActionButtonPressedActionType
    {
        get; set
        {
            field = value;

            ActionButtonAction = field switch
            {
                ActionType.Nothing => null,
                ActionType.UploadChanges => "Upload changes",
                ActionType.DownloadChanges => "Download changes",
                ActionType.TakeInUse => "Take in use",
                _ => throw new ArgumentOutOfRangeException(nameof(ActionButtonPressedActionType), ActionButtonPressedActionType, null)
            };
        }
    }

    public Func<LocalSaveInfoViewModel, CancellationToken, Task>? OnActionButtonClicked;
    
    [RelayCommand]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        bool delete = await modalService.ShowAsync("Are you sure?",
            $"Are you sure you want to delete {Name}?", "Yes", "No", cancellationToken);

        if (!delete)
            return;

        bool deleteFromFiles = await modalService.ShowAsync("Delete local files?",
            $"Would you like to delete the local files for {Name} as well?", "Yes", "No", cancellationToken);

        if (deleteFromFiles)
        {
            LocalSaveInfo? localSaveEntry = saveCatalogService.GetLocalSave(Id);

            if (localSaveEntry is null)
                throw new SaveNotFoundException();
            
            Directory.Delete(localSaveEntry.LocalPath, true);
        }
        
        await saveCatalogService.DeleteLocalSave(Id, cancellationToken);
    }

    [RelayCommand]
    private async Task OnActionButtonPressed(CancellationToken cancellationToken)
    {
        if (OnActionButtonClicked is not null)
            await OnActionButtonClicked.Invoke(this, cancellationToken);
    }

    [RelayCommand]
    private async Task BrowseAsync(CancellationToken cancellationToken)
    {
        LocalSaveInfo? localSave = saveCatalogService.GetLocalSave(Id);
        if (localSave is null)
            throw new SaveNotFoundException();
        
        await mainWindowProvider.MainWindow.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(localSave.LocalPath));
    }
}