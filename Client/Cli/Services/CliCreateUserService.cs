using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Commands.Services;

public class CliCreateUserService : ICreateUserService
{
    public async Task<CreateUserResult?> ShowAsync(string? error, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("No user found. Creating a new one. Press Ctrl+C to cancel.");
        Console.Write("Enter Username: ");
        string userName = Console.ReadLine() ?? throw new OperationCanceledException();
        CreateUserResult result = new(userName);
        return result;
    }
}