using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Services;

public class ServerStatusService(IConnectionManager connectionManager) : IServerStatusService
{
    public bool IsConnectedToServer => connectionManager.IsConnected;
    event Func<CancellationToken, Task>? IServerStatusService.ConnectionStatusChanged
    {
        add => connectionManager.ConnectionStatusChanged += value;
        remove => connectionManager.ConnectionStatusChanged -= value;
    }
    
    public event Func<CancellationToken, Task>? Connected
    {
        add => connectionManager.Connected += value;
        remove => connectionManager.Connected -= value;
    }
}