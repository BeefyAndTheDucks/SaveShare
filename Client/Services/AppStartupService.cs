using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Client.Interfaces;

namespace Client.Services;

public sealed class AppStartupService(IConnectionManager connectionManager, ISaveCatalogService saveCatalogService, ILoginCoordinator loginCoordinator) : IAppStartupService
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await connectionManager.ConnectAsync(
            new Uri("ws://server.dev.localhost:5144/v1/ws"),
            cancellationToken);

        try
        {
            await loginCoordinator.SignInOrCreateUserAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) 
                throw new OperationCanceledException("User cancelled login");
            desktop.Shutdown();
            return;
        }

        await saveCatalogService.RefreshAsync(cancellationToken);
    }
}