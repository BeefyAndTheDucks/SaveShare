using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Services;
using Common;

namespace Client.Interfaces;

public interface IAuthenticationService
{
    User? CurrentUser { get; }
    bool IsAuthenticated => CurrentUser != null;

    event EventHandler<User?>? UserChanged;
    
    Task<User?> TrySignInAsync(CancellationToken cancellationToken = default);
    Task<User?> CreateNewUserAsync(string userName, CancellationToken cancellationToken = default);
}
