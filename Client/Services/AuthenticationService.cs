using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Interfaces;
using Common;
using Common.Protocol.V1;

namespace Client.Services;

public class AuthenticationService(IServerSession serverSession, IUserStore userStore) : IAuthenticationService
{
    public User? CurrentUser { get; private set; }
    public event EventHandler<User?>? UserChanged;

    public async Task<User?> TrySignInAsync(CancellationToken cancellationToken = default)
    {
        User? storedUser = await userStore.LoadAsync(cancellationToken);
        if (storedUser is null)
            return null;

        try
        {
            S2CSuccessfullySignedInMessage signedIn =
                await serverSession.SignInAsExistingUserAsync(storedUser.Id, cancellationToken);

            CurrentUser = storedUser with { Username = signedIn.UserName };
            UserChanged?.Invoke(this, CurrentUser);

            await userStore.SaveAsync(CurrentUser, cancellationToken);
            return CurrentUser;
        }
        catch (ServerErrorException e)
        {
            if (e.Error.Code == ErrorCode.FailedToAuthenticate)
                await userStore.ClearAsync(cancellationToken);
            return null;
        }
    }

    public async Task<User?> CreateNewUserAsync(string userName, CancellationToken cancellationToken = default)
    {
        S2CNewUserCreatedMessage created = await serverSession.SignInAsNewUserAsync(userName, cancellationToken);

        CurrentUser = new User(created.Id, userName);
        UserChanged?.Invoke(this, CurrentUser);
        
        await userStore.SaveAsync(CurrentUser, cancellationToken);
        return CurrentUser;
    }
}