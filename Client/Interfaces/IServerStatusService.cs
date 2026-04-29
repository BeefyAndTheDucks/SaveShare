using System;

namespace Client.Interfaces;

public interface IServerStatusService
{
    bool IsConnectedToServer { get; }
    event EventHandler? ConnectionComplete;
}