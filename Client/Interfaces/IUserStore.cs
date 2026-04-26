using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface IUserStore
{
    Task<User?> LoadAsync(CancellationToken cancellationToken = default);
    
    Task SaveAsync(User user, CancellationToken cancellationToken = default);
    
    Task ClearAsync(CancellationToken cancellationToken = default);
}