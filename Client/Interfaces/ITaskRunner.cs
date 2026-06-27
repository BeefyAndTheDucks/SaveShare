using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface ITaskRunner
{
    Task RunAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default);
}