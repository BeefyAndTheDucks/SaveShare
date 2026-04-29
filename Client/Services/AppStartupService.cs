using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Client.Interfaces;

namespace Client.Services;

public sealed class AppStartupService(IConnectionManager connectionManager, ISaveCatalogService saveCatalogService, ILoginCoordinator loginCoordinator, ISettingsStore settingsStore, IInitialSetupService initialSetupService) : IAppStartupService
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        AppSettings? appSettings = await settingsStore.LoadAsync(cancellationToken);

        if (appSettings == null || string.IsNullOrWhiteSpace(appSettings.ServerUri.ToString()))
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                while (!(lifetime.MainWindow?.IsVisible ?? false))
                    await Task.Yield();
            }
            SetupResult? result = await initialSetupService.ShowAsync(null, cancellationToken);
            if (result is null)
                return;
            appSettings = new AppSettings(result.ServerUri);
            await settingsStore.SaveAsync(appSettings, cancellationToken);
        }
        
        await connectionManager.ConnectAsync(
            appSettings.ServerUri,
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