using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Common;

namespace Client.Services;

public class SaveSyncService(IServerSession serverSession, ISaveCatalogService saveCatalogService, ILocalSavesStore localSavesStore) : ISaveSyncService
{
    public async Task<SaveInfo> AddLocalSaveAsync(string savePath, string saveName, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        SaveInfo registeredSave = await serverSession.RegisterNewSaveAsync(saveName, cancellationToken);
        await localSavesStore.AddOrUpdateAsync(LocalSaveEntry.FromSave(registeredSave, savePath), cancellationToken);
        await saveCatalogService.RefreshAsync(cancellationToken);
        await OverwriteCloudSaveAsync(registeredSave.SaveId, savePath, progress, cancellationToken);
        return registeredSave;
    }

    public async Task OverwriteCloudSaveAsync(SaveId saveId, string savePath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long compressedTotal = await DirectoryPacker.GetPackedSize(savePath, cancellationToken);
        
        IProgress<long>? byteProgress = ByteProgressToNormalizedProgress.From(progress, compressedTotal);

        await serverSession.OverwriteSaveDataAsync(saveId, async (stream, ct) =>
        {
            await DirectoryPacker.PackDirectory(savePath, stream, ct);
        }, byteProgress, cancellationToken);
    }

    public async Task DownloadCloudSaveAsync(SaveId saveId, string targetPath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Result<SaveInfo> save = saveCatalogService.GetSaveInfo(saveId);
        if (!save.Check())
            return;
        await localSavesStore.AddOrUpdateAsync(LocalSaveEntry.FromSave(save.Value, targetPath), cancellationToken);
        await CheckoutCloudSaveAsync(saveId, targetPath, progress, cancellationToken);
        
        await serverSession.DownloadSaveAsync(saveId, async (stream, ct) =>
        {
            await DirectoryPacker.UnpackDirectory(stream, targetPath, ct);
        }, byteCount => ByteProgressToNormalizedProgress.From(progress, byteCount), cancellationToken);
    }

    public async Task CheckoutCloudSaveAsync(SaveId saveId, string targetPath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await serverSession.CheckoutSaveAsync(saveId, cancellationToken);
    }

    public async Task ForceReleaseAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await serverSession.ForceReleaseAsync(saveId, cancellationToken);
    }
}

public class ByteProgressToNormalizedProgress(IProgress<double> normalizedProgress, long byteCount) : IProgress<long>
{
    public static ByteProgressToNormalizedProgress? From(IProgress<double>? normalizedProgress, long byteCount)
    {
        return normalizedProgress != null ? new ByteProgressToNormalizedProgress(normalizedProgress, byteCount) : null;
    } 
    
    public void Report(long byteProgress)
    {
        normalizedProgress.Report(byteProgress / (double)byteCount);
    }
}
