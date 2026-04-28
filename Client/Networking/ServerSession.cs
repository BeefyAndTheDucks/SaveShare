using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Interfaces;
using Common;

namespace Client.Networking;

public sealed class ServerSession(ITransport transport) : IServerSession
{
    public event Func<SaveInfo[], CancellationToken, Task>? SavesChanged;
    public bool IsConnected => transport.IsConnected;

    private readonly SemaphoreSlim _operationLock = new(1, 1);

    #region Helpers
    private async Task<TMessage> ExpectAsync<TMessage>(CancellationToken cancellationToken)
        where TMessage : S2CMessage
    {
        while (true)
        {
            S2CMessage message = await transport.ReceiveMessageAsync(cancellationToken);

            switch (message)
            {
                case TMessage expected:
                    return expected;

                case S2CErrorMessage error:
                    throw new ServerErrorException(error);

                case S2CSavesChangedMessage savesChanged:
                    SavesChanged?.Invoke(savesChanged.Saves, cancellationToken);
                    continue;

                default:
                    throw new UnexpectedServerMessageException(message.Type);
            }
        }
    }
    
    private async Task<TStopMessage> ExpectManyAsync<TMessage, TStopMessage>(Action<TMessage> messageReceived, CancellationToken cancellationToken)
        where TMessage : S2CMessage
    {
        while (true)
        {
            S2CMessage message = await transport.ReceiveMessageAsync(cancellationToken);

            switch (message)
            {
                case TStopMessage stopMessage:
                    return stopMessage;
                
                case TMessage expected:
                    messageReceived(expected);
                    break;

                case S2CErrorMessage error:
                    throw new ServerErrorException(error);

                case S2CSavesChangedMessage savesChanged:
                    SavesChanged?.Invoke(savesChanged.Saves, cancellationToken);
                    continue;

                default:
                    throw new UnexpectedServerMessageException(message.Type);
            }
        }
    }
    
    private async Task<TMessage> SendAndExpectAsync<TMessage>(
        C2SMessage request,
        CancellationToken cancellationToken)
        where TMessage : S2CMessage
    {
        await transport.SendMessageAsync(request, cancellationToken);
        return await ExpectAsync<TMessage>(cancellationToken);
    }
    
    #endregion
    
    public async Task ConnectAsync(Uri server, CancellationToken cancellationToken = default)
    {
        await transport.ConnectAsync(server, cancellationToken);
    }

    public async Task<S2CSuccessfullySignedInMessage> SignInAsExistingUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            return await SendAndExpectAsync<S2CSuccessfullySignedInMessage>(new C2SSignInAsExistingUserMessage(userId),
                cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<S2CNewUserCreatedMessage> SignInAsNewUserAsync(string userName, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            return await SendAndExpectAsync<S2CNewUserCreatedMessage>(new C2SSignInAsNewUserMessage(userName), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<SaveInfo[]> ListSavesAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            S2CSaveListMessage savesMessage = await SendAndExpectAsync<S2CSaveListMessage>(new C2SListSavesMessage(), cancellationToken);
            return savesMessage.Saves;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<SaveInfo> RegisterNewSaveAsync(string name, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            S2CRegisteredNewSaveMessage registeredMessage = await SendAndExpectAsync<S2CRegisteredNewSaveMessage>(new C2SRegisterNewSaveMessage(name), cancellationToken);
            return registeredMessage.CreatedSaveInfo;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task OverwriteSaveDataAsync(SaveId saveId, Func<Stream, CancellationToken, Task> writeSaveDataAsync, IProgress<long>? bytesSent = null,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            await SendAndExpectAsync<S2CReadyForBinaryDataMessage>(new C2SOverwriteSaveDataMessage(saveId), cancellationToken);
            await transport.SendBinaryAsync(writeSaveDataAsync, bytesSent, cancellationToken);
            await ExpectAsync<S2CSuccessMessage>(cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task CheckoutSaveAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            await SendAndExpectAsync<S2CSuccessMessage>(new C2SCheckoutSaveMessage(saveId), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task DownloadSaveAsync(SaveId saveId, Func<Stream, CancellationToken, Task> readSaveDataAsync, Func<long, IProgress<long>?>? bytesReceived = null,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            S2CReadyToSendBinaryDataMessage readyToSendMessage = await SendAndExpectAsync<S2CReadyToSendBinaryDataMessage>(new C2SDownloadSaveMessage(saveId), cancellationToken);
            await transport.SendMessageAsync(new C2SReadyForBinaryDataMessage(), cancellationToken);
            await transport.ReceiveBinaryAsync(readSaveDataAsync, bytesReceived?.Invoke(readyToSendMessage.ByteCount), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task ForceReleaseAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            await SendAndExpectAsync<S2CSuccessMessage>(new C2SForceReleaseMessage(saveId), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task ReleaseAsync(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);

        try
        {
            await SendAndExpectAsync<S2CSuccessMessage>(new C2SReleaseMessage(saveId), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task DownloadSaveChangesAsync(SaveId saveId,
        Func<Stream, CancellationToken, Task> readSignaturesDataAsync,
        Func<Stream, CancellationToken, Task> writeDeltasDataAsync,
        IProgress<long>? sendSignaturesProgress, 
        IProgress<double>? createDeltasProgress, 
        Func<long, IProgress<long>?>? receiveDeltasProgress,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            await SendAndExpectAsync<S2CReadyForBinaryDataMessage>(new C2SDownloadSaveChangesMessage(saveId), cancellationToken);
            await transport.SendBinaryAsync(readSignaturesDataAsync, sendSignaturesProgress, cancellationToken);
            S2CReadyToSendBinaryDataMessage readyToSendMessage =
                await ExpectManyAsync<S2CProgressMessage, S2CReadyToSendBinaryDataMessage>(
                    msg => createDeltasProgress?.Report(msg.Progress), cancellationToken);
            await transport.SendMessageAsync(new C2SReadyForBinaryDataMessage(), cancellationToken);
            await transport.ReceiveBinaryAsync(writeDeltasDataAsync, receiveDeltasProgress?.Invoke(readyToSendMessage.ByteCount), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task UploadSaveChangesAsync(SaveId saveId,
        Func<Stream, CancellationToken, Task> readSignaturesAsync,
        Func<Stream, CancellationToken, Task> writeDeltasAsync,
        IProgress<double>? createSignaturesProgress, Func<long, IProgress<long>?>? receiveSignaturesProgress,
        IProgress<long>? sendDeltasProgress, IProgress<double>? applyDeltasProgress,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            await transport.SendMessageAsync(new C2SUploadSaveChangesMessage(saveId), cancellationToken);
            S2CReadyToSendBinaryDataMessage readyToSendMessage =
                await ExpectManyAsync<S2CProgressMessage, S2CReadyToSendBinaryDataMessage>(
                    msg => createSignaturesProgress?.Report(msg.Progress), cancellationToken);
            await transport.SendMessageAsync(new C2SReadyForBinaryDataMessage(), cancellationToken);
            await transport.ReceiveBinaryAsync(readSignaturesAsync, receiveSignaturesProgress?.Invoke(readyToSendMessage.ByteCount), cancellationToken);
            await SendAndExpectAsync<S2CReadyForBinaryDataMessage>(new C2SReadyToSendBinaryDataMessage(), cancellationToken);
            await transport.SendBinaryAsync(writeDeltasAsync, sendDeltasProgress, cancellationToken);
            await ExpectManyAsync<S2CProgressMessage, S2CSuccessMessage>(msg => applyDeltasProgress?.Report(msg.Progress), cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }
}
