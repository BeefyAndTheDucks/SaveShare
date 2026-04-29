using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Storage;

public sealed class SettingsStore(IAppDataPaths paths, IFileStore fileStore) : ISettingsStore
{
    public async Task<AppSettings?> LoadAsync(CancellationToken cancellationToken = default)
    {
        return await fileStore.ReadAsync<AppSettings>(paths.AppSettingsFilePath, cancellationToken) ?? default;
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        return fileStore.WriteAsync(paths.AppSettingsFilePath, settings, cancellationToken);
    }
}