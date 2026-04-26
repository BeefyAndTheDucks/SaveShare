using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ITransport
{
    bool IsConnected { get; }
    
    Task ConnectAsync(Uri server, CancellationToken cancellationToken = default);
    Task SendMessageAsync(C2SMessage message, CancellationToken cancellationToken = default);
    Task<S2CMessage> ReceiveMessageAsync(CancellationToken cancellationToken = default);
    Task SendBinaryAsync(Func<Stream, CancellationToken, Task> writeAsync, IProgress<long>? bytesSent = null, CancellationToken cancellationToken = default);
    Task ReceiveBinaryAsync(Func<Stream, CancellationToken, Task> readAsync, IProgress<long>? bytesReceived = null, CancellationToken cancellationToken = default);
}
