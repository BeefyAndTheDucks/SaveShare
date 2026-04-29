using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Common;

namespace Client.Networking;

public sealed class WebSocketTransport(IMessageCodec messageCodec) : ITransport, IAsyncDisposable
{
    private ClientWebSocket? WebSocket { get; set; }

    public bool IsConnected => WebSocket?.State == WebSocketState.Open;
    public event EventHandler? ConnectionComplete;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _receiveLock = new(1, 1);

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (IsConnected)
            throw new InvalidOperationException("Already connected.");

        try
        {
            WebSocket = new ClientWebSocket();
            await WebSocket.ConnectAsync(uri, cancellationToken);
            Console.WriteLine("Connected to server");
        }
        finally
        {
            ConnectionComplete?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task SendMessageAsync(C2SMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected.");
        
        string json = messageCodec.Serialize(message);
        await _sendLock.WaitAsync(cancellationToken);
        await WebSocket!.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancellationToken);
        _sendLock.Release();
    }

    public async Task<S2CMessage> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected.");

        await _receiveLock.WaitAsync(cancellationToken);
        string json = await WebSocketUtils.ReceiveString(WebSocket!, cancellationToken);
        _receiveLock.Release();
        return messageCodec.Deserialize(json);
    }

    public async Task SendBinaryAsync(Func<Stream, CancellationToken, Task> writeAsync, IProgress<long>? bytesSent = null, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected.");
        
        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            Stream stream = WebSocketStream.Create(WebSocket!, WebSocketMessageType.Binary);

            if (bytesSent is not null)
                stream = new ProgressStream(stream, bytesSent.Report);
            
            await writeAsync(stream, cancellationToken);
            
            await stream.DisposeAsync();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task ReceiveBinaryAsync(Func<Stream, CancellationToken, Task> readAsync, IProgress<long>? bytesReceived, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected.");
        
        await _receiveLock.WaitAsync(cancellationToken);

        try
        {
            Stream stream = WebSocketStream.Create(WebSocket!, WebSocketMessageType.Binary);

            if (bytesReceived is not null)
                stream = new ProgressStream(stream, bytesReceived.Report);

            await readAsync(stream, cancellationToken);
            
            //byte[] drainBuffer = new byte[1024];
            //while (await stream.ReadAsync(drainBuffer, cancellationToken) > 0) { }
            await stream.DisposeAsync();
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
            if (IsConnected)
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client shutting down", CancellationToken.None);
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