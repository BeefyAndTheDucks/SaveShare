using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IAppStartupService
{
    Task StartAsync(CancellationToken cancellationToken = default);
}