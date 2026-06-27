using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Services;

public sealed class TaskRunner(IErrorPresenter errorPresenter) : ITaskRunner
{
    public async Task RunAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default)
    {
        try
        {
            await task(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User cancellation; usually no dialog.
        }
        catch (Exception ex)
        {
            await errorPresenter.ShowErrorAsync(ex, cancellationToken);
        }
    }
}