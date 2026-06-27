using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IConnectionManager
{
    bool IsConnected { get; }
    event Func<CancellationToken, Task>? ConnectionStatusChanged;
    event Func<CancellationToken, Task>? Connected;

    Task ConnectAsync(
        Uri server,
        CancellationToken cancellationToken = default);
}
