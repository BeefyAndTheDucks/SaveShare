using System;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ISaveCatalogService
{
    SaveInfo[] CloudSaves { get; }
    LocalSaveEntry[] LocalSaves { get; }
    
    event EventHandler? SavesChanged;

    Task RefreshAsync(CancellationToken cancellationToken = default);
    Task RefreshCloudSavesAsync(CancellationToken cancellationToken = default);
    Task RefreshLocalSavesAsync(CancellationToken cancellationToken = default);
    
    Result<SaveInfo> GetSaveInfo(SaveId saveId);
    Task DeleteLocalSave(SaveId saveId, CancellationToken cancellationToken = default);
    LocalSaveEntry? GetLocalSave(SaveId saveId);
}