using System;
using Client.Interfaces;

namespace Client.Services;

public class ServerStatusService(IConnectionManager connectionManager) : IServerStatusService
{
    public bool IsConnectedToServer => connectionManager.IsConnected;
    event EventHandler? IServerStatusService.ConnectionComplete
    {
        add => connectionManager.ConnectionComplete += value;
        remove => connectionManager.ConnectionComplete -= value;
    }
}