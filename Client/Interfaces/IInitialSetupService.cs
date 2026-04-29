using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IInitialSetupService
{
    Task<SetupResult?> ShowAsync(string? error, CancellationToken cancellationToken = default);
}

public sealed record SetupResult(Uri ServerUri);
