using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Common;

namespace Client.Services;

public class SaveSyncService(IServerSession serverSession, ISaveCatalogService saveCatalogService, ILocalSavesStore localSavesStore) : ISaveSyncService
{
    public bool IsBusy => _tasksInProgress > 0;

    private int _tasksInProgress;

    private void BeginTask()
    {
        _tasksInProgress++;
    }

    private void EndTask()
    {
        _tasksInProgress--;
    }

    public async Task<SaveInfo> AddLocalSaveAsync(string savePath, string saveName, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            SaveInfo registeredSave = await serverSession.RegisterNewSaveAsync(saveName, cancellationToken);
            await localSavesStore.AddOrUpdateAsync(LocalSaveInfo.FromSave(registeredSave, savePath), cancellationToken);
            await saveCatalogService.RefreshAsync(cancellationToken);
            await OverwriteCloudSaveAsync(registeredSave.SaveId, savePath, progress, cancellationToken);
            return registeredSave;
        }
        finally
        {
            EndTask();
        }
    }

    public async Task OverwriteCloudSaveAsync(SaveId saveId, string savePath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            long compressedTotal = await DirectoryPacker.GetPackedSizeAsync(savePath, cancellationToken);
        
            IProgress<long>? byteProgress = ByteProgressToNormalizedProgress.From(progress, compressedTotal);

            await serverSession.OverwriteSaveDataAsync(saveId, async (stream, ct) =>
            {
                await DirectoryPacker.PackDirectoryAsync(savePath, stream, ct);
            }, byteProgress, cancellationToken);
        
            await saveCatalogService.RefreshAsync(cancellationToken);
        }
        finally
        {
            EndTask();
        }
    }

    public async Task DownloadCloudSaveAsync(SaveId saveId, string targetPath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            Result<SaveInfo> save = saveCatalogService.GetSaveInfo(saveId);
            if (!save.Check())
                return;
            await localSavesStore.AddOrUpdateAsync(LocalSaveInfo.FromSave(save.Value, targetPath), cancellationToken);
            await CheckoutCloudSaveAsync(saveId, cancellationToken);

            await serverSession.DownloadSaveAsync(saveId, async (stream, ct) =>
            {
                await DirectoryPacker.UnpackDirectoryAsync(stream, targetPath, ct: ct);
            }, byteCount => ByteProgressToNormalizedProgress.From(progress, byteCount), cancellationToken);

            await saveCatalogService.RefreshAsync(cancellationToken);
        }
        finally
        {
            EndTask();
        }
    }

    public async Task CheckoutCloudSaveAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            await serverSession.CheckoutSaveAsync(saveId, cancellationToken);
            await saveCatalogService.RefreshAsync(cancellationToken);
        }
        finally
        {
            EndTask();
        }
    }

    public async Task ForceReleaseAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            await serverSession.ForceReleaseAsync(saveId, cancellationToken);
            await saveCatalogService.RefreshAsync(cancellationToken);
        }
        finally
        {
            EndTask();
        }
    }

    public async Task DownloadCloudSaveChangesAsync(SaveId saveId, IProgress<double>? createManifestProgress = null,
        IProgress<double>? buildSignaturesProgress = null, IProgress<double>? sendSignaturesProgress = null, 
        IProgress<double>? buildDeltasProgress = null, IProgress<double>? receiveDeltasProgress = null, 
        IProgress<double>? applyDeltasProgress = null, CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            LocalSaveInfo? localSaveInfo = saveCatalogService.GetLocalSave(saveId);
            if (localSaveInfo is null)
                return;

            ByteProgressToNormalizedProgress? sendProgress = ByteProgressToNormalizedProgress.From(sendSignaturesProgress, 999999);

            AggregateProgress? buildManifestAggregateProgress = AggregateProgress.From(createManifestProgress);
        
            DirectoryManifest? cloudManifest = null;
            DirectoryManifest localManifest = await DirectoryManifest.From(localSaveInfo.LocalPath, buildManifestAggregateProgress?.CreateProgressItem(), cancellationToken);
        
            await CheckoutCloudSaveAsync(saveId, cancellationToken);
            await serverSession.DownloadSaveChangesAsync(saveId, localManifest, m => cloudManifest = m, async (stream, token) =>
                {
                    if (cloudManifest is null)
                        throw new InvalidOperationException("Manifest was not received.");
                
                    await DirectoryPacker.BuildAndPackSignaturesAsync(localSaveInfo.LocalPath, () => stream, cloudManifest,
                        localManifest, buildSignaturesProgress, (byteSize, _) =>
                        {
                            sendProgress?.ChangeByteCount(byteSize);
                            return Task.CompletedTask;
                        }, false, token);
                }, async (stream, token) =>
                {
                    if (cloudManifest is null)
                        throw new InvalidOperationException("Manifest was not received.");
                
                    await DirectoryPacker.ApplyDeltasAsync(localSaveInfo.LocalPath, stream, cloudManifest, localManifest, applyDeltasProgress, token);
                }, buildManifestAggregateProgress?.CreateProgressItem(), sendProgress, buildDeltasProgress,
            byteCount => ByteProgressToNormalizedProgress.From(receiveDeltasProgress, byteCount), cancellationToken);
            await saveCatalogService.RefreshAsync(cancellationToken);
        }
        finally
        {
            EndTask();
        }
    }

    public async Task UploadLocalSaveChangesAsync(SaveId saveId, IProgress<double>? buildManifestProgress = null,
        IProgress<double>? buildSignaturesProgress = null, IProgress<double>? receiveSignaturesProgress = null, 
        IProgress<double>? buildDeltasProgress = null, IProgress<double>? sendDeltasProgress = null, 
        IProgress<double>? applyDeltasProgress = null, CancellationToken cancellationToken = default)
    {
        BeginTask();
        try
        {
            LocalSaveInfo? localSaveInfo = saveCatalogService.GetLocalSave(saveId);
            if (localSaveInfo is null)
                return;
        
            ByteProgressToNormalizedProgress? sendProgress = ByteProgressToNormalizedProgress.From(sendDeltasProgress, 999999);
        
            AggregateProgress? buildManifestAggregateProgress = AggregateProgress.From(buildManifestProgress);
        
            DirectoryManifest? cloudManifest = null;
            DirectoryManifest localManifest = await DirectoryManifest.From(localSaveInfo.LocalPath, buildManifestAggregateProgress?.CreateProgressItem(), cancellationToken);
        
            await using MemoryStream deltas = new();
        
            await serverSession.UploadSaveChangesAsync(saveId, localManifest, m => cloudManifest = m, async (signatureStream, token) =>
                {
                    if (cloudManifest is null)
                        throw new InvalidOperationException("Manifest was not received.");
                
                    await DirectoryPacker.CreateDeltasAsync(localSaveInfo.LocalPath, signatureStream, deltas, localManifest, cloudManifest, buildDeltasProgress, (byteCount, _) =>
                    {
                        sendProgress?.ChangeByteCount(byteCount);
                        return Task.CompletedTask;
                    }, token);
                }, (deltaStream, _) =>
                {
                    deltas.WriteTo(deltaStream);
                    return Task.CompletedTask;
                }, buildManifestAggregateProgress?.CreateProgressItem(),
                buildSignaturesProgress, byteCount => ByteProgressToNormalizedProgress.From(receiveSignaturesProgress, byteCount),
                sendProgress, applyDeltasProgress, cancellationToken);
        
            await serverSession.ReleaseAsync(saveId, cancellationToken);
            await saveCatalogService.RefreshAsync(cancellationToken);
        }
        finally
        {
            EndTask();
        }
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
