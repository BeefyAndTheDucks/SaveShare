using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface IServerSession
{
    event Func<SaveInfo[], CancellationToken, Task>? SavesChanged;
    
    bool IsConnected { get; }
    
    Task ConnectAsync(Uri server, CancellationToken cancellationToken = default);
    
    Task<S2CSuccessfullySignedInMessage> SignInAsExistingUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    Task<S2CNewUserCreatedMessage> SignInAsNewUserAsync(
        string userName,
        CancellationToken cancellationToken = default);
    
    Task<SaveInfo[]> ListSavesAsync(
        CancellationToken cancellationToken = default);
    
    Task<SaveInfo> RegisterNewSaveAsync(
        string name,
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
}
