using System;
using System.IO;
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
        await localSavesStore.AddOrUpdateAsync(LocalSaveInfo.FromSave(registeredSave, savePath), cancellationToken);
        await saveCatalogService.RefreshAsync(cancellationToken);
        await OverwriteCloudSaveAsync(registeredSave.SaveId, savePath, progress, cancellationToken);
        return registeredSave;
    }

    public async Task OverwriteCloudSaveAsync(SaveId saveId, string savePath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long compressedTotal = await DirectoryPacker.GetPackedSizeAsync(savePath, cancellationToken);
        
        IProgress<long>? byteProgress = ByteProgressToNormalizedProgress.From(progress, compressedTotal);

        await serverSession.OverwriteSaveDataAsync(saveId, async (stream, ct) =>
        {
            await DirectoryPacker.PackDirectoryAsync(savePath, stream, ct);
        }, byteProgress, cancellationToken);
        
        await saveCatalogService.RefreshAsync(cancellationToken);
    }

    public async Task DownloadCloudSaveAsync(SaveId saveId, string targetPath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Result<SaveInfo> save = saveCatalogService.GetSaveInfo(saveId);
        if (!save.Check())
            return;
        await localSavesStore.AddOrUpdateAsync(LocalSaveInfo.FromSave(save.Value, targetPath), cancellationToken);
        await CheckoutCloudSaveAsync(saveId, cancellationToken);
        
        await serverSession.DownloadSaveAsync(saveId, async (stream, ct) =>
        {
            await DirectoryPacker.UnpackDirectoryAsync(stream, targetPath, ct);
        }, byteCount => ByteProgressToNormalizedProgress.From(progress, byteCount), cancellationToken);
        
        await saveCatalogService.RefreshAsync(cancellationToken);
    }

    public async Task CheckoutCloudSaveAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await serverSession.CheckoutSaveAsync(saveId, cancellationToken);
        await saveCatalogService.RefreshAsync(cancellationToken);
    }

    public async Task ForceReleaseAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await serverSession.ForceReleaseAsync(saveId, cancellationToken);
        await saveCatalogService.RefreshAsync(cancellationToken);
    }

    public async Task DownloadCloudSaveChangesAsync(SaveId saveId, IProgress<double>? buildSignaturesProgress = null,
        IProgress<double>? sendSignaturesProgress = null, IProgress<double>? buildDeltasProgress = null,
        IProgress<double>? receiveDeltasProgress = null, IProgress<double>? applyDeltasProgress = null,
        CancellationToken cancellationToken = default)
    {
        LocalSaveInfo? localSaveInfo = saveCatalogService.GetLocalSave(saveId);
        if (localSaveInfo is null)
            return;

        ByteProgressToNormalizedProgress? sendProgress = ByteProgressToNormalizedProgress.From(sendSignaturesProgress, 999999);
        
        await CheckoutCloudSaveAsync(saveId, cancellationToken);
        await serverSession.DownloadSaveChangesAsync(saveId, async (stream, token) =>
        {
            await DirectoryPacker.BuildAndPackSignaturesAsync(localSaveInfo.LocalPath, () => stream, buildSignaturesProgress,
                (byteSize, _) =>
                {
                    sendProgress?.ChangeByteCount(byteSize);
                    return Task.CompletedTask;
                }, false, token);
        }, async (stream, token) =>
        {
            await DirectoryPacker.ApplyDeltasAsync(localSaveInfo.LocalPath, stream, applyDeltasProgress, token);
        }, sendProgress, buildDeltasProgress,
        byteCount => ByteProgressToNormalizedProgress.From(receiveDeltasProgress, byteCount), cancellationToken);
        await saveCatalogService.RefreshAsync(cancellationToken);
    }

    public async Task UploadLocalSaveChangesAsync(SaveId saveId, IProgress<double>? buildSignaturesProgress = null,
        IProgress<double>? receiveSignaturesProgress = null, IProgress<double>? buildDeltasProgress = null,
        IProgress<double>? sendDeltasProgress = null, IProgress<double>? applyDeltasProgress = null,
        CancellationToken cancellationToken = default)
    {
        LocalSaveInfo? localSaveInfo = saveCatalogService.GetLocalSave(saveId);
        if (localSaveInfo is null)
            return;
        
        ByteProgressToNormalizedProgress? sendProgress = ByteProgressToNormalizedProgress.From(sendDeltasProgress, 999999);
        
        await using MemoryStream deltas = new();
        
        await serverSession.UploadSaveChangesAsync(saveId, async (signatureStream, token) =>
            {
                await DirectoryPacker.CreateDeltasAsync(localSaveInfo.LocalPath, signatureStream, deltas, buildDeltasProgress, (byteCount, _) =>
                {
                    sendProgress?.ChangeByteCount(byteCount);
                    return Task.CompletedTask;
                }, token);
            }, (deltaStream, _) =>
            {
                deltas.WriteTo(deltaStream);
                return Task.CompletedTask;
            },
            buildSignaturesProgress, byteCount => ByteProgressToNormalizedProgress.From(receiveSignaturesProgress, byteCount),
            sendProgress, applyDeltasProgress, cancellationToken);
        
        await serverSession.ReleaseAsync(saveId, cancellationToken);
        await saveCatalogService.RefreshAsync(cancellationToken);
    }
}

public class ByteProgressToNormalizedProgress(IProgress<double> normalizedProgress, long byteCount) : IProgress<long>
{
    private long _byteCount = byteCount;

    public static ByteProgressToNormalizedProgress? From(IProgress<double>? normalizedProgress, long byteCount)
    {
        return normalizedProgress != null ? new ByteProgressToNormalizedProgress(normalizedProgress, byteCount) : null;
    }
    
    public void ChangeByteCount(long byteCount) => _byteCount = byteCount;
    
    public void Report(long byteProgress)
    {
        normalizedProgress.Report(byteProgress / (double)_byteCount);
    }
}
