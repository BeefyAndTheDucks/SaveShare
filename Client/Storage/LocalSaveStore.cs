using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Common;

namespace Client.Storage;

public sealed class LocalSavesStore(IAppDataPaths paths, IFileStore fileStore) : ILocalSavesStore
{
    public event Func<LocalSaveInfo[], CancellationToken, Task>? SavesChanged;

    public async Task<LocalSaveInfo[]> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await fileStore.ReadAsync<LocalSaveInfo[]>(paths.LocalSavesFilePath, cancellationToken) ?? [];
    }

    public async Task SaveAsync(IReadOnlyCollection<LocalSaveInfo> saves, CancellationToken cancellationToken = default)
    {
        await fileStore.WriteAsync(paths.LocalSavesFilePath, saves, cancellationToken);
        
        SavesChanged?.Invoke(saves.ToArray(), cancellationToken);
    }

    public async Task AddOrUpdateAsync(LocalSaveInfo save, CancellationToken cancellationToken = default)
    {
        List<LocalSaveInfo> saves = [..await LoadAsync(cancellationToken)];
        int index = saves.FindIndex(existing => existing.SaveId == save.SaveId);
        
        if (index >= 0)
            saves[index] = save;
        else
            saves.Add(save);
        
        await SaveAsync(saves, cancellationToken);
    }

    public async Task RemoveAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        LocalSaveInfo[] saves = await LoadAsync(cancellationToken);

        await SaveAsync(
            saves.Where(save => save.SaveId != saveId).ToArray(),
            cancellationToken);
    }
}