using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Services;

public sealed class TaskRunner(IErrorPresenter errorPresenter) : ITaskRunner
{
    public async Task<bool> RunAsync(Func<CancellationToken, Task> task, CancellationToken cancellationToken = default)
    {
        try
        {
            await task(cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            await errorPresenter.ShowErrorAsync(ex, cancellationToken);
            return false;
        }
    }
}