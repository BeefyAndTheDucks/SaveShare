using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Networking;

public class ConnectionManager(IServerSession serverSession) : IConnectionManager
{
    public bool IsConnected => serverSession.IsConnected;
    public event Func<CancellationToken, Task>? ConnectionStatusChanged
    {
        add => serverSession.ConnectionStatusChanged += value;
        remove => serverSession.ConnectionStatusChanged -= value;
    }
    
    public event Func<CancellationToken, Task>? Connected
    {
        add => serverSession.Connected += value;
        remove => serverSession.Connected -= value;
    }

    public async Task ConnectAsync(Uri server, CancellationToken cancellationToken = default)
    {
        await serverSession.ConnectAsync(new Uri(server, "v1/ws"), cancellationToken);
    }
}