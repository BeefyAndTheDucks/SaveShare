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
    public event Func<LocalSaveEntry[], CancellationToken, Task>? SavesChanged;

    public async Task<LocalSaveEntry[]> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await fileStore.ReadAsync<LocalSaveEntry[]>(paths.LocalSavesFilePath, cancellationToken) ?? [];
    }

    public async Task SaveAsync(IReadOnlyCollection<LocalSaveEntry> saves, CancellationToken cancellationToken = default)
    {
        await fileStore.WriteAsync(paths.LocalSavesFilePath, saves, cancellationToken);
        
        SavesChanged?.Invoke(saves.ToArray(), cancellationToken);
    }

    public async Task AddOrUpdateAsync(LocalSaveEntry save, CancellationToken cancellationToken = default)
    {
        List<LocalSaveEntry> saves = [..await LoadAsync(cancellationToken)];
        int index = saves.FindIndex(existing => existing.SaveId == save.SaveId);
        
        if (index >= 0)
            saves[index] = save;
        else
            saves.Add(save);
        
        await SaveAsync(saves, cancellationToken);
    }

    public async Task RemoveAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        LocalSaveEntry[] saves = await LoadAsync(cancellationToken);

        await SaveAsync(
            saves.Where(save => save.SaveId != saveId).ToArray(),
            cancellationToken);
    }
}