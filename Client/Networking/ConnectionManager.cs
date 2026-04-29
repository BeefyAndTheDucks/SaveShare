using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Networking;

public class ConnectionManager(IServerSession serverSession) : IConnectionManager
{
    public bool IsConnected => serverSession.IsConnected;
    public event EventHandler? ConnectionComplete
    {
        add => serverSession.ConnectionComplete += value;
        remove => serverSession.ConnectionComplete -= value;
    }

    public async Task ConnectAsync(Uri server, CancellationToken cancellationToken = default)
    {
        await serverSession.ConnectAsync(new Uri(server, "v1/ws"), cancellationToken);
    }
}