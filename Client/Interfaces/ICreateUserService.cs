using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface ICreateUserService
{
    Task<CreateUserResult?> ShowAsync(string? error, CancellationToken cancellationToken = default);
}

public sealed record CreateUserResult(string UserName);