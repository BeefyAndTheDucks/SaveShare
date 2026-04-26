using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Interfaces;
using Common;

namespace Client.Services;

public sealed class LoginCoordinator(IAuthenticationService authService, ICreateUserService createUserService) : ILoginCoordinator
{
    public async Task<User> SignInOrCreateUserAsync(CancellationToken cancellationToken = default)
    {
        User? existingUser = await authService.TrySignInAsync(cancellationToken);
        
        if (existingUser is not null)
            return existingUser;

        return await TryCreateUserAsync(cancellationToken: cancellationToken);
    }

    private async Task<User> TryCreateUserAsync(string? error = null, CancellationToken cancellationToken = default)
    {
        CreateUserResult? createUserResult = await createUserService.ShowAsync(error, cancellationToken);
        
        if (createUserResult is null)
            throw new OperationCanceledException(
                "User creation was cancelled.",
                cancellationToken);

        try
        {
            User? createdUser = await authService.CreateNewUserAsync(
                createUserResult.UserName,
                cancellationToken);
            if (createdUser is null)
                return await TryCreateUserAsync("Failed to create user", cancellationToken);
            return createdUser;
        }
        catch (ServerErrorException ex)
        {
            return await TryCreateUserAsync(ex.Error.Message, cancellationToken);
        }
    }
}