using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Commands.Services;

public class CliInitialSetupService : IInitialSetupService
{
    public Task<SetupResult?> ShowAsync(string? error, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.Write("Welcome. Please enter a server URI: ");

            Uri uri = new(Console.ReadLine() ?? throw new OperationCanceledException());
        
            SetupResult result = new(uri);
            return Task.FromResult(result)!;
        }
        catch (Exception exception)
        {
            return Task.FromException<SetupResult?>(exception);
        }
    }
}