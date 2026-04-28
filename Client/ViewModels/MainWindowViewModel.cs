using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Client.Interfaces;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

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
    
    private readonly Dictionary<string, LocalSaveInfoViewModel> _pendingSaves = new(StringComparer.OrdinalIgnoreCase);

    public MainWindowViewModel()
    {
        _authenticationService = null!;
        _saveSyncService = null!;
        _selectSaveForUploadService = null!;
        _saveCatalogService = null!;
        _selectSaveForDownloadService = null!;
    }
    
    public MainWindowViewModel(IAuthenticationService authenticationService, ISaveSyncService saveSyncService,
        ISelectSaveForUploadService selectSaveForUploadService, ISaveCatalogService saveCatalogService,
        ISelectSaveForDownloadService selectSaveForDownloadService)
    {
        _authenticationService = authenticationService;
        _saveSyncService = saveSyncService;
        _selectSaveForUploadService = selectSaveForUploadService;
        _selectSaveForDownloadService = selectSaveForDownloadService;
        _saveCatalogService = saveCatalogService;
        
        _authenticationService.UserChanged += OnUserChanged;
        _saveCatalogService.SavesChanged += SaveCatalogServiceOnSavesChanged;
        
        if (_authenticationService.CurrentUser is not null)
            Username = _authenticationService.CurrentUser.Username;
    }

    private bool IsCurrentUser(string? username) => username == _authenticationService.CurrentUser?.Username;

    private string? FormatUsernameString(string? baseUsername)
    {
        string? formattedUsername = baseUsername;
        if (string.IsNullOrWhiteSpace(baseUsername))
            formattedUsername = null;

        if (IsCurrentUser(baseUsername))
            formattedUsername = "you";
        return formattedUsername;
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

                string? inUseBy = FormatUsernameString(relatedCloudSave?.CheckedOutByUserName);
                string? lastChangedBy = FormatUsernameString(relatedCloudSave?.LastSyncedByUserName);
                
                bool isInUseByCurrentUser = IsCurrentUser(relatedCloudSave?.CheckedOutByUserName);
                bool lastChangedByCurrentUser = IsCurrentUser(relatedCloudSave?.LastSyncedByUserName);

                LocalSaveInfoViewModel.ActionType buttonAction = LocalSaveInfoViewModel.ActionType.Nothing;
                if (string.IsNullOrEmpty(relatedCloudSave?.CheckedOutByUserName))
                    buttonAction = LocalSaveInfoViewModel.ActionType.TakeInUse;
                if (!lastChangedByCurrentUser && !isInUseByCurrentUser)
                    buttonAction = LocalSaveInfoViewModel.ActionType.DownloadChanges;
                if (isInUseByCurrentUser)
                    buttonAction = LocalSaveInfoViewModel.ActionType.UploadChanges;
                
                LocalSaveInfoViewModel vm = App.Services.GetRequiredService<LocalSaveInfoViewModel>();
                vm.Name = s.Name;
                vm.Id = s.SaveId;
                vm.InUseBy = inUseBy;
                vm.ExistsOnServer = existsOnServer;
                vm.LastChangedBy = lastChangedBy;
                vm.LastChangedAt = relatedCloudSave?.LastSyncedAt;
                vm.ActionButtonPressedActionType = buttonAction;
                vm.OnActionButtonClicked += SelectedLocalSaveOnActionButtonClicked;
                return vm;
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
                    existing.LastChangedAt = newItem.LastChangedAt;
                    existing.LastChangedBy = newItem.LastChangedBy;
                    existing.InUseBy = newItem.InUseBy;
                    existing.OnActionButtonClicked = newItem.OnActionButtonClicked;
                    existing.ActionButtonPressedActionType = newItem.ActionButtonPressedActionType;
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

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken)
    {
        return _saveCatalogService.RefreshAsync(cancellationToken);
    }

    private Task SelectedLocalSaveOnActionButtonClicked(LocalSaveInfoViewModel vm, CancellationToken cancellationToken = default)
    {
        return vm.ActionButtonPressedActionType switch
        {
            LocalSaveInfoViewModel.ActionType.Nothing => Task.CompletedTask,
            LocalSaveInfoViewModel.ActionType.UploadChanges => UploadChanges(vm, cancellationToken),
            LocalSaveInfoViewModel.ActionType.DownloadChanges => DownloadChanges(vm, cancellationToken),
            LocalSaveInfoViewModel.ActionType.TakeInUse => TakeInUse(vm, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(vm.ActionButtonPressedActionType), vm.ActionButtonPressedActionType, null)
        };
    }

    private async Task TakeInUse(LocalSaveInfoViewModel vm, CancellationToken cancellationToken)
    {
        await _saveSyncService.CheckoutCloudSaveAsync(vm.Id, cancellationToken);
    }

    private async Task DownloadChanges(LocalSaveInfoViewModel vm, CancellationToken cancellationToken)
    {
        vm.CurrentOperation = "Downloading changes...";
        vm.CurrentOperationProgress = 0.0;
        vm.CurrentSubOperation = "Preparing...";
        vm.CurrentSubOperationIndeterminate = true;

        double index = 0;
        LocalSaveViewModelProgress buildSignaturesProgress = new(vm, index / 5, index++ / 5, "Building signatures...");
        LocalSaveViewModelProgress sendSignaturesProgress = new(vm, index / 5, index++ / 5, "Sending signatures...");
        LocalSaveViewModelProgress buildDeltasProgress = new(vm, index / 5, index++ / 5, "Server - Building deltas...");
        LocalSaveViewModelProgress receiveDeltasProgress = new(vm, index / 5, index++ / 5, "Receiving deltas...");
        LocalSaveViewModelProgress applyDeltasProgress = new(vm, index / 5, index++ / 5, "Applying deltas...");

        await _saveSyncService.DownloadCloudSaveChangesAsync(vm.Id, buildSignaturesProgress, sendSignaturesProgress,
            buildDeltasProgress, receiveDeltasProgress, applyDeltasProgress, cancellationToken);
        vm.CurrentOperation = null;
        vm.CurrentSubOperation = null;
    }

    private async Task UploadChanges(LocalSaveInfoViewModel vm, CancellationToken cancellationToken)
    {
        vm.CurrentOperation = "Uploading changes...";
        vm.CurrentOperationProgress = 0.0;
        vm.CurrentSubOperation = "Preparing...";
        vm.CurrentSubOperationIndeterminate = true;

        double index = 0;
        LocalSaveViewModelProgress buildSignaturesProgress = new(vm, index / 5, index++ / 5, "Server - Building signatures...");
        LocalSaveViewModelProgress receiveSignaturesProgress = new(vm, index / 5, index++ / 5, "Receiving signatures...");
        LocalSaveViewModelProgress buildDeltasProgress = new(vm, index / 5, index++ / 5, "Building deltas...");
        LocalSaveViewModelProgress sendDeltasProgress = new(vm, index / 5, index++ / 5, "Sending deltas...");
        LocalSaveViewModelProgress applyDeltasProgress = new(vm, index / 5, index++ / 5, "Server - Applying deltas...");
        
        await _saveSyncService.UploadLocalSaveChangesAsync(vm.Id, buildSignaturesProgress, receiveSignaturesProgress,
            buildDeltasProgress, sendDeltasProgress, applyDeltasProgress, cancellationToken);
        vm.CurrentOperation = null;
        vm.CurrentSubOperation = null;
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
        
        LocalSaveViewModelProgress progress = new(shadowVm, 0.1, 1.0, progressOperation);
        
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

    private LocalSaveInfoViewModel CreateShadowVm(TaskSaveInfo saveInfo, string operation, double operationProgress,
        string initialSubOperation = "Preparing...", bool initialSubOperationIndeterminate = true)
    {
        LocalSaveInfoViewModel vm = App.Services.GetRequiredService<LocalSaveInfoViewModel>();
        
        vm.Id = saveInfo.SaveId;
        vm.Name = saveInfo.SaveName;
        vm.CurrentOperation = operation;
        vm.CurrentOperationProgress = operationProgress;
        vm.CurrentSubOperation = initialSubOperation;
        vm.CurrentSubOperationIndeterminate = initialSubOperationIndeterminate;
        vm.OnActionButtonClicked += SelectedLocalSaveOnActionButtonClicked;

        return vm;
    }
    #endregion
}

public class LocalSaveViewModelProgress(LocalSaveInfoViewModel viewModel, double overallProgressBegin, double overallProgressEnd, string subOperationName) : IProgress<double>
{
    public void Report(double value)
    {
        viewModel.CurrentOperationProgress = double.Lerp(overallProgressBegin, overallProgressEnd, value);
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
