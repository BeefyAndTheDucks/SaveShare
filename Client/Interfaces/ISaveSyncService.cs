using System;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ISaveSyncService
{
    Task<SaveInfo> AddLocalSaveAsync(
        string savePath, 
        string saveName,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task OverwriteCloudSaveAsync(
        SaveId saveId,
        string savePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
    
    Task DownloadCloudSaveAsync(
        SaveId saveId,
        string targetPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
    
    Task CheckoutCloudSaveAsync(SaveId saveId,
        CancellationToken cancellationToken = default);
    
    Task ForceReleaseAsync(
        SaveId saveId,
        CancellationToken cancellationToken = default);
    
    Task DownloadCloudSaveChangesAsync(
        SaveId saveId,
        IProgress<double>? buildSignaturesProgress = null,
        IProgress<double>? sendSignaturesProgress = null,
        IProgress<double>? buildDeltasProgress = null,
        IProgress<double>? receiveDeltasProgress = null,
        IProgress<double>? applyDeltasProgress = null,
        CancellationToken cancellationToken = default);
    
    Task UploadLocalSaveChangesAsync(
        SaveId saveId,
        IProgress<double>? buildSignaturesProgress = null,
        IProgress<double>? receiveSignaturesProgress = null,
        IProgress<double>? buildDeltasProgress = null,
        IProgress<double>? sendDeltasProgress = null,
        IProgress<double>? applyDeltasProgress = null,
        CancellationToken cancellationToken = default);
}