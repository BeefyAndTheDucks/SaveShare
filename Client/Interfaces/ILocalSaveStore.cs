using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ILocalSavesStore
{
    event Func<LocalSaveEntry[], CancellationToken, Task>? SavesChanged;
    
    Task<LocalSaveEntry[]> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyCollection<LocalSaveEntry> saves, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(LocalSaveEntry save, CancellationToken cancellationToken = default);
    Task RemoveAsync(SaveId saveId, CancellationToken cancellationToken = default);
}

public sealed record LocalSaveEntry(
    SaveId SaveId,
    string Name,
    string LocalPath)
{
    public static LocalSaveEntry FromSave(SaveInfo save, string localPath) => new(save.SaveId, save.Name, localPath);
}