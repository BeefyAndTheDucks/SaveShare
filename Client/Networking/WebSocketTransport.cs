using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Interfaces;
using Common;
using Common.Protocol.V1;

namespace Client.Networking;

public sealed class WebSocketTransport(IMessageCodec messageCodec) : ITransport, IAsyncDisposable
{
    private ClientWebSocket? WebSocket { get; set; }

    private Uri? LastUsedUri { get; set; }

    public bool IsConnected => WebSocket?.State == WebSocketState.Open && !_lostConnectionReported;
    public event Func<CancellationToken, Task>? ConnectionStatusChanged;
    public event Func<CancellationToken, Task>? ConnectedEarly;
    public event Func<CancellationToken, Task>? Connected;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _receiveLock = new(1, 1);

    private bool _lostConnectionReported;

    private async Task<ClientWebSocket> GetConnectedWebSocket(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            await Reconnect(cancellationToken: cancellationToken);

        if (WebSocket is null)
            throw new TransportException("Cannot get connected WebSocket: No WebSocket.");
        
        return WebSocket;
    }

    private async Task Reconnect(Exception? ex = null, CancellationToken cancellationToken = default)
    {
        if (LastUsedUri is null)
            throw new TransportException("Cannot reconnect: No last used URI.");
        
        try
        {
            await ConnectAsync(LastUsedUri, cancellationToken: cancellationToken);
        }
        catch (TransportException)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
    }

    private async Task<TransportException> ReportConnectionLostAndCreateException(Exception? innerException = null, CancellationToken cancellationToken = default)
    {
        _lostConnectionReported = true;
        
        if (!IsConnected && ConnectionStatusChanged is not null)
            await ConnectionStatusChanged.Invoke(cancellationToken);
        
        return new TransportException("Lost connection to the server.", innerException);
    }

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            throw new TransportException("Cannot connect to server: Already connected.");

        LastUsedUri = uri;

        try
        {
            WebSocket = new ClientWebSocket();
            await WebSocket.ConnectAsync(uri, cancellationToken);
            Console.WriteLine("Connected to server");

            _lostConnectionReported = false;

            if (ConnectedEarly is not null)
                await ConnectedEarly.Invoke(cancellationToken);

            if (Connected is not null)
                await Connected.Invoke(cancellationToken);
        }
        finally
        {
            if (ConnectionStatusChanged is not null)
                await ConnectionStatusChanged.Invoke(cancellationToken);
        }
    }

    public async Task DisconnectAsync(WebSocketCloseStatus reason = WebSocketCloseStatus.NormalClosure, string? message = null, CancellationToken cancellationToken = default)
    {
        if (WebSocket is null)
            return;
        
        await WebSocket.CloseAsync(reason, message, cancellationToken);
    }

    public async Task SendMessageAsync(C2SMessage message, CancellationToken cancellationToken = default)
    {
        ClientWebSocket webSocket = await GetConnectedWebSocket(cancellationToken);
        
        string json = messageCodec.Serialize(message);
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true,
                cancellationToken);
        }
        catch (WebSocketException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (IOException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (ObjectDisposedException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (InvalidOperationException ex) when (WebSocket?.State != WebSocketState.Open)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<S2CMessage> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        ClientWebSocket webSocket = await GetConnectedWebSocket(cancellationToken);

        await _receiveLock.WaitAsync(cancellationToken);
        try
        {
            string json = await WebSocketUtils.ReceiveString(webSocket, cancellationToken);
            if (webSocket.State != WebSocketState.Open)
                throw await ReportConnectionLostAndCreateException(cancellationToken: cancellationToken);

            return messageCodec.Deserialize(json);
        }
        catch (WebSocketException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (IOException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (ObjectDisposedException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (InvalidOperationException ex) when (WebSocket?.State != WebSocketState.Open)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        finally
        {
            _receiveLock.Release();
        }
    }

    public async Task SendBinaryAsync(Func<Stream, CancellationToken, Task> writeAsync, IProgress<long>? bytesSent = null, CancellationToken cancellationToken = default)
    {
        ClientWebSocket webSocket = await GetConnectedWebSocket(cancellationToken);
        
        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            Stream stream = WebSocketStream.Create(webSocket, WebSocketMessageType.Binary);

            if (bytesSent is not null)
                stream = new ProgressStream(stream, bytesSent.Report);
            
            await writeAsync(stream, cancellationToken);
            
            await stream.DisposeAsync();
        }
        catch (WebSocketException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (IOException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (ObjectDisposedException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (InvalidOperationException ex) when (WebSocket?.State != WebSocketState.Open)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task ReceiveBinaryAsync(Func<Stream, CancellationToken, Task> readAsync, IProgress<long>? bytesReceived, CancellationToken cancellationToken = default)
    {
        ClientWebSocket webSocket = await GetConnectedWebSocket(cancellationToken);
        
        await _receiveLock.WaitAsync(cancellationToken);

        try
        {
            Stream stream = WebSocketStream.Create(webSocket, WebSocketMessageType.Binary);

            if (bytesReceived is not null)
                stream = new ProgressStream(stream, bytesReceived.Report);

            await readAsync(stream, cancellationToken);
            
            await stream.DisposeAsync();

            if (webSocket.State != WebSocketState.Open)
                throw await ReportConnectionLostAndCreateException(cancellationToken: cancellationToken);
        }
        catch (WebSocketException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (IOException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (ObjectDisposedException ex)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        catch (InvalidOperationException ex) when (WebSocket?.State != WebSocketState.Open)
        {
            throw await ReportConnectionLostAndCreateException(ex, cancellationToken);
        }
        finally
        {
            _receiveLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_sendLock);
        await CastAndDispose(_receiveLock);

        if (WebSocket != null)
        {
            if (WebSocket.State == WebSocketState.Open)
                await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Client shutting down", CancellationToken.None);
            await CastAndDispose(WebSocket);
        }

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}
