using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Interfaces;
using Common;

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

            CurrentUser = new User(storedUser.Id, signedIn.UserName);
            UserChanged?.Invoke(this, CurrentUser);

            await userStore.SaveAsync(CurrentUser, cancellationToken);
            return CurrentUser;
        }
        catch (ServerErrorException)
        {
            await userStore.ClearAsync(cancellationToken);
            return null;
        }
        
        /*Guid userId = Guid.Parse("da70178d-bf2d-4be6-b11d-979616f981aa");
        S2CSuccessfullySignedInMessage signedInMessage = await serverSession.SignInAsExistingUserAsync(userId, cancellationToken);
        CurrentUser = new User(userId, signedInMessage.UserName);
        UserChanged?.Invoke(this, CurrentUser);
        return CurrentUser;*/
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