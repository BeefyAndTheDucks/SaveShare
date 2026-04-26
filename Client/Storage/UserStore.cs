using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Common;

namespace Client.Storage;

public sealed class UserStore(IAppDataPaths paths, IFileStore fileStore) : IUserStore
{
    public Task<User?> LoadAsync(CancellationToken cancellationToken = default)
    {
        return fileStore.ReadAsync<User>(paths.UserFilePath, cancellationToken);
    }

    public Task SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        return fileStore.WriteAsync(paths.UserFilePath, user, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return fileStore.DeleteAsync(paths.UserFilePath, cancellationToken);
    }
}