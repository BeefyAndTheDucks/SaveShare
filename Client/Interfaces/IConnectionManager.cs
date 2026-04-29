using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IConnectionManager
{
    bool IsConnected { get; }
    event EventHandler? ConnectionComplete;

    Task ConnectAsync(
        Uri server,
        CancellationToken cancellationToken = default);
}
