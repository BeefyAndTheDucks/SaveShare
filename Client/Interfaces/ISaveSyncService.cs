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
    
    Task CheckoutCloudSaveAsync(
        SaveId saveId,
        string targetPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
    
    Task ForceReleaseAsync(
        SaveId saveId,
        CancellationToken cancellationToken = default);
}