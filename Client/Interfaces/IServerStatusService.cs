using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IServerStatusService
{
    bool IsConnectedToServer { get; }
    event Func<CancellationToken, Task>? ConnectionStatusChanged;
    event Func<CancellationToken, Task>? Connected;
}