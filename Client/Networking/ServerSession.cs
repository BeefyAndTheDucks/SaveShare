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
}
