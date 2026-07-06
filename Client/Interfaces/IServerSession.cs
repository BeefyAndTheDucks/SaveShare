using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Protocol.V1;

namespace Client.Interfaces;

public interface IServerSession
{
    event Func<SaveInfo[], CancellationToken, Task>? SavesChanged;
    
    bool IsConnected { get; }
    event Func<CancellationToken, Task>? ConnectionStatusChanged;
    event Func<CancellationToken, Task>? Connected;
    
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    
    Task<S2CSuccessfullySignedInMessage> SignInAsExistingUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    Task<S2CNewUserCreatedMessage> SignInAsNewUserAsync(
        string userName,
        CancellationToken cancellationToken = default);
    
    Task<SaveInfo[]> ListSavesAsync(
        CancellationToken cancellationToken = default);
    
    Task<SaveInfo> RegisterNewSaveAsync(string name,
        SaveType saveType,
        string sourceFileExtension,
        CancellationToken cancellationToken = default);
    
    Task OverwriteSaveDataAsync(
        SaveId saveId,
        Func<Stream, CancellationToken, Task> writeSaveDataAsync,
        IProgress<long>? bytesSent = null,
        CancellationToken cancellationToken = default);
    
    Task CheckoutSaveAsync(
        SaveId saveId,
        CancellationToken cancellationToken = default);
    
    Task DownloadSaveAsync(
        SaveId saveId,
        Func<Stream, CancellationToken, Task> readSaveDataAsync,
        Func<long, IProgress<long>?>? bytesReceived = null,
        CancellationToken cancellationToken = default);
    
    Task ForceReleaseAsync(
        SaveId saveId,
        CancellationToken cancellationToken = default);
    
    Task ReleaseAsync(
        SaveId saveId,
        CancellationToken cancellationToken = default);

    Task DownloadSaveChangesAsync(SaveId saveId,
        SaveManifest localManifest,
        Action<SaveManifest> onManifestReceived,
        Func<Stream, CancellationToken, Task> readSignaturesDataAsync,
        Func<Stream, CancellationToken, Task> writeDeltasDataAsync,
        IProgress<double>? createManifestProgress,
        IProgress<long>? sendSignaturesProgress,
        IProgress<double>? createDeltasProgress,
        Func<long, IProgress<long>?>? receiveDeltasProgress,
        CancellationToken cancellationToken = default);

    Task UploadSaveChangesAsync(SaveId saveId,
        SaveManifest manifest,
        Action<SaveManifest> onManifestReceived,
        Func<Stream, CancellationToken, Task> readSignaturesAsync,
        Func<Stream, CancellationToken, Task> writeDeltasAsync,
        IProgress<double>? createManifestProgress,
        IProgress<double>? createSignaturesProgress,
        Func<long, IProgress<long>?>? receiveSignaturesProgress,
        IProgress<long>? sendDeltasProgress,
        IProgress<double>? applyDeltasProgress,
        CancellationToken cancellationToken = default);
}
