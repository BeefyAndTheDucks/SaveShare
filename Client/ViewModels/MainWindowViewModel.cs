using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Client.Exceptions;
using Client.Interfaces;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string? Username { get; set; } = "SIGNING IN...";
    
    [ObservableProperty]
    public partial CloudSaveInfoViewModel[]? CloudSaves { get; set; }
    
    [ObservableProperty]
    public partial LocalSaveInfoViewModel[]? LocalSaves { get; set; }
    
    [ObservableProperty]
    public partial LocalSaveInfoViewModel? SelectedLocalSave { get; set; }
    
    private readonly IAuthenticationService _authenticationService;
    private readonly ISaveSyncService _saveSyncService;
    private readonly ISelectSaveForUploadService _selectSaveForUploadService;
    private readonly ISaveCatalogService _saveCatalogService;
    private readonly ISelectSaveForDownloadService _selectSaveForDownloadService;
    private readonly IModalService _modalService;
    
    private readonly Dictionary<string, LocalSaveInfoViewModel> _pendingSaves = new(StringComparer.OrdinalIgnoreCase);
    
    public MainWindowViewModel(IAuthenticationService authenticationService, ISaveSyncService saveSyncService,
        ISelectSaveForUploadService selectSaveForUploadService, ISaveCatalogService saveCatalogService,
        ISelectSaveForDownloadService selectSaveForDownloadService, IModalService modalService)
    {
        _authenticationService = authenticationService;
        _saveSyncService = saveSyncService;
        _selectSaveForUploadService = selectSaveForUploadService;
        _selectSaveForDownloadService = selectSaveForDownloadService;
        _modalService = modalService;
        _saveCatalogService = saveCatalogService;
        
        _authenticationService.UserChanged += OnUserChanged;
        _saveCatalogService.SavesChanged += SaveCatalogServiceOnSavesChanged;
        
        if (_authenticationService.CurrentUser is not null)
            Username = _authenticationService.CurrentUser.Username;
    }

    private void SaveCatalogServiceOnSavesChanged(object? sender, EventArgs e)
    {
        CloudSaves = _saveCatalogService.CloudSaves.Select(s => new CloudSaveInfoViewModel { Name =  s.Name, SaveId = s.SaveId }).ToArray();

        SaveId? selectedId = SelectedLocalSave?.Id;

        List<LocalSaveInfoViewModel> freshList = _saveCatalogService.LocalSaves
            .Select(s =>
            {
                SaveInfo? relatedCloudSave = _saveCatalogService.CloudSaves
                    .Cast<SaveInfo?>()
                    .FirstOrDefault(cs => cs!.Value.SaveId == s.SaveId);
                bool existsOnServer = relatedCloudSave.HasValue;

                string? inUseBy = relatedCloudSave?.CheckedOutByUserName;
                if (string.IsNullOrWhiteSpace(inUseBy))
                    inUseBy = null;
                
                return new LocalSaveInfoViewModel { Name = s.Name, Id = s.SaveId, InUseBy = inUseBy, ExistsOnServer = existsOnServer };
            })
            .ToList();

        if (LocalSaves != null)
        {
            for (int i = 0; i < freshList.Count; i++)
            {
                LocalSaveInfoViewModel newItem = freshList[i];
                LocalSaveInfoViewModel? existing = LocalSaves.FirstOrDefault(old => old.Id == newItem.Id);

                if (existing == null && _pendingSaves.TryGetValue(newItem.Name, out LocalSaveInfoViewModel? pendingVm))
                {
                    if (selectedId == pendingVm.Id)
                        selectedId = newItem.Id;
                    
                    existing = pendingVm;
                    _pendingSaves.Remove(newItem.Name);
                }

                if (existing != null)
                {
                    existing.Id = newItem.Id;
                    existing.ExistsOnServer = newItem.ExistsOnServer;
                    existing.Name = newItem.Name;
                    freshList[i] = existing;
                }
            }
        }
        
        LocalSaves = freshList.ToArray();
        
        if (selectedId != null)
            SelectedLocalSave = LocalSaves.FirstOrDefault(s => s.Id == selectedId);
    }

    private void OnUserChanged(object? sender, User? user)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Username = user?.Username ?? "NOT SIGNED IN";
        });
    }

    [RelayCommand]
    private async Task DeleteLocalSaveAsync(CancellationToken cancellationToken)
    {
        if (SelectedLocalSave is null)
            return;

        bool delete = await _modalService.ShowAsync("Are you sure?",
            $"Are you sure you want to delete {SelectedLocalSave.Name}?", "Yes", "No", cancellationToken);

        if (!delete)
            return;

        bool deleteFromFiles = await _modalService.ShowAsync("Delete local files?",
            $"Would you like to delete the local files for {SelectedLocalSave.Name} as well?", "Yes", "No", cancellationToken);

        if (deleteFromFiles)
        {
            LocalSaveEntry? localSaveEntry = _saveCatalogService.GetLocalSave(SelectedLocalSave.Id);

            if (localSaveEntry is null)
                throw new SaveNotFoundException();
            
            Directory.Delete(localSaveEntry.LocalPath, true);
        }
        
        await _saveCatalogService.DeleteLocalSave(SelectedLocalSave.Id, cancellationToken);
    }
    
    [RelayCommand]
    private async Task AddLocalSaveAsync(CancellationToken cancellationToken)
    {
        SelectSaveForUploadResult? result = await _selectSaveForUploadService.ShowAsync(cancellationToken);

        if (result is null)
            return;
        
        await RunTaskAsync(result.SaveName, "Uploading save...", "Transferring data...", async (progress, token) =>
        {
            await _saveSyncService.AddLocalSaveAsync(result.SavePath, result.SaveName, progress, token);
        }, cancellationToken: cancellationToken);
    }
    
    [RelayCommand]
    private async Task AddCloudSaveAsync(CancellationToken cancellationToken)
    {
        SelectSaveForDownloadResult? result = await _selectSaveForDownloadService.ShowAsync(cancellationToken);

        if (result is null)
            return;

        await RunTaskAsync(result.SaveInfo, "Downloading save...", "Transferring data...", async (progress, token) =>
        {
            await _saveSyncService.DownloadCloudSaveAsync(result.SaveInfo.SaveId, result.TargetPath, progress, token);
        }, cancellationToken: cancellationToken);
    }
    
    #region Task System
    private Task RunTaskAsync(string saveName, string operation, string progressOperation,
        Func<IProgress<double>, CancellationToken, Task> task, string initialSubOperation = "Preparing...",
        Action<TaskInfo>? taskInfoCreated = null, CancellationToken cancellationToken = default)
    {
        return RunTaskAsync(new TaskSaveInfo(Guid.NewGuid(), saveName), operation, progressOperation, task,
            initialSubOperation, info =>
            {
                _pendingSaves[saveName] = info.ViewModel;
                taskInfoCreated?.Invoke(info);
            }, cancellationToken);
    }

    private async Task RunTaskAsync(TaskSaveInfo saveInfo, string operation, string progressOperation,
        Func<IProgress<double>, CancellationToken, Task> task, string initialSubOperation = "Preparing...",
        Action<TaskInfo>? taskInfoCreated = null, CancellationToken cancellationToken = default)
    {
        TaskInfo taskInfo = StartTask(saveInfo, operation, progressOperation, initialSubOperation);
        taskInfoCreated?.Invoke(taskInfo);

        try
        {
            await task(taskInfo.Progress, cancellationToken);
        }
        catch (Exception)
        {
            TaskOnException(taskInfo);
            throw;
        }
        finally
        {
            EndTask(taskInfo);
        }
    }

    private record TaskSaveInfo(SaveId SaveId, string SaveName)
    {
        public static implicit operator TaskSaveInfo(SaveInfo saveInfo)
        {
            return new TaskSaveInfo(saveInfo.SaveId, saveInfo.Name);
        }
    }
    private record TaskInfo(LocalSaveInfoViewModel ViewModel, IProgress<double> Progress, TaskSaveInfo SaveInfo);

    private TaskInfo StartTask(TaskSaveInfo saveInfo, string operation, string progressOperation, string initialSubOperation = "Preparing...")
    {
        LocalSaveInfoViewModel shadowVm = CreateShadowVm(saveInfo, operation, 0.1, initialSubOperation);

        LocalSaves = LocalSaves?.Append(shadowVm).ToArray() ?? [shadowVm];
        
        SelectedLocalSave = shadowVm;
        
        LocalSaveViewModelProgress progress = new(shadowVm, 0.5, progressOperation);
        
        return new TaskInfo(shadowVm, progress, saveInfo);
    }

    private void TaskOnException(TaskInfo task)
    {
        LocalSaves = LocalSaves?.Where(s => s.Id != task.SaveInfo.SaveId).ToArray();
        _pendingSaves.Remove(task.SaveInfo.SaveName);
    }

    private static void EndTask(TaskInfo task)
    {
        task.ViewModel.CurrentOperation = null;
        task.ViewModel.CurrentSubOperation = null;
    }

    private static LocalSaveInfoViewModel CreateShadowVm(TaskSaveInfo saveInfo, string operation, double operationProgress,
        string initialSubOperation = "Preparing...", bool initialSubOperationIndeterminate = true)
    {
        return new LocalSaveInfoViewModel
        {
            Id = saveInfo.SaveId,
            Name = saveInfo.SaveName,
            CurrentOperation = operation,
            CurrentOperationProgress = operationProgress,
            CurrentSubOperation = initialSubOperation,
            CurrentSubOperationIndeterminate = initialSubOperationIndeterminate
        };
    }
    #endregion
}

public class LocalSaveViewModelProgress(LocalSaveInfoViewModel viewModel, double overallProgress, string subOperationName) : IProgress<double>
{
    public void Report(double value)
    {
        viewModel.CurrentOperationProgress = overallProgress;
        viewModel.CurrentSubOperation = subOperationName;
        viewModel.CurrentSubOperationProgress = value;
        viewModel.CurrentSubOperationIndeterminate = false;
    }
}

public class ConsoleProgress : IProgress<double>
{
    public void Report(double value)
    {
        Console.WriteLine(value);
    }
}
