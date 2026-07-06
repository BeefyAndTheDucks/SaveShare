using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Protocol.V1;

namespace Client.Interfaces;

public interface ITransport
{
    bool IsConnected { get; }
    event Func<CancellationToken, Task>? ConnectionStatusChanged;
    event Func<CancellationToken, Task>? ConnectedEarly;
    event Func<CancellationToken, Task>? Connected;
    
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task DisconnectAsync(WebSocketCloseStatus reason = WebSocketCloseStatus.NormalClosure, string? message = null, CancellationToken cancellationToken = default);
    Task SendMessageAsync(C2SMessage message, CancellationToken cancellationToken = default);
    Task<S2CMessage> ReceiveMessageAsync(CancellationToken cancellationToken = default);
    Task SendBinaryAsync(Func<Stream, CancellationToken, Task> writeAsync, IProgress<long>? bytesSent = null, CancellationToken cancellationToken = default);
    Task ReceiveBinaryAsync(Func<Stream, CancellationToken, Task> readAsync, IProgress<long>? bytesReceived = null, CancellationToken cancellationToken = default);
}
