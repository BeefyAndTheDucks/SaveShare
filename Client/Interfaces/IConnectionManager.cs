using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IConnectionManager
{
    bool IsConnected { get; }

    Task ConnectAsync(
        Uri server,
        CancellationToken cancellationToken = default);
}
