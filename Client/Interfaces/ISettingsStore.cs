using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public sealed record AppSettings(
    Uri ServerUri);