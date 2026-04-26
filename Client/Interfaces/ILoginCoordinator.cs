using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ILoginCoordinator
{
    Task<User> SignInOrCreateUserAsync(CancellationToken cancellationToken = default);
}