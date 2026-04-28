using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ILocalSavesStore
{
    event Func<LocalSaveInfo[], CancellationToken, Task>? SavesChanged;
    
    Task<LocalSaveInfo[]> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyCollection<LocalSaveInfo> saves, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(LocalSaveInfo save, CancellationToken cancellationToken = default);
    Task RemoveAsync(SaveId saveId, CancellationToken cancellationToken = default);
}

public sealed record LocalSaveInfo(
    SaveId SaveId,
    string Name,
    string LocalPath)
{
    public static LocalSaveInfo FromSave(SaveInfo save, string localPath) => new(save.SaveId, save.Name, localPath);
}