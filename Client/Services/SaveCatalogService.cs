using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Common;

namespace Client.Services;

public class SaveCatalogService : ISaveCatalogService
{
    private readonly IServerSession _serverSession;
    private readonly ILocalSavesStore _localSavesStore;
    
    public SaveInfo[] CloudSaves { get; private set; }
    public LocalSaveEntry[] LocalSaves { get; private set; }
    public event EventHandler? SavesChanged;
    
    public SaveCatalogService(IServerSession serverSession, ILocalSavesStore localSavesStore)
    {
        _serverSession = serverSession;
        _localSavesStore = localSavesStore;
        CloudSaves = [];
        LocalSaves = [];
        
        _serverSession.SavesChanged += ServerSessionOnSavesChanged;
        _localSavesStore.SavesChanged += LocalSavesStoreOnSavesChanged;
    }

    private Task LocalSavesStoreOnSavesChanged(LocalSaveEntry[] localSaveEntries, CancellationToken cancellationToken)
    {
        LocalSaves = localSaveEntries;
        SavesChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private Task ServerSessionOnSavesChanged(SaveInfo[] saves, CancellationToken cancellationToken)
    {
        CloudSaves = saves;
        SavesChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        LocalSaves = await _localSavesStore.LoadAsync(cancellationToken);
        CloudSaves = await _serverSession.ListSavesAsync(cancellationToken);
        SavesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshCloudSavesAsync(CancellationToken cancellationToken = default)
    {
        await _serverSession.ListSavesAsync(cancellationToken);
        SavesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RefreshLocalSavesAsync(CancellationToken cancellationToken = default)
    {
        await _serverSession.ListSavesAsync(cancellationToken);
        SavesChanged?.Invoke(this, EventArgs.Empty);
    }

    public Result<SaveInfo> GetSaveInfo(SaveId saveId)
    {
        SaveInfo? save = CloudSaves.FirstOrDefault(save => save.SaveId == saveId);
        if (save is null)
            return Result<SaveInfo>.Failure($"SaveInfo for SaveId {saveId} not found.");
        return save;
    }

    public async Task DeleteLocalSave(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await _localSavesStore.RemoveAsync(saveId, cancellationToken);
        SavesChanged?.Invoke(this, EventArgs.Empty);
    }

    public LocalSaveEntry? GetLocalSave(SaveId saveId)
    {
        return LocalSaves.FirstOrDefault(s => s.SaveId == saveId);
    }
}