using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Commands.Services;
using Client.Interfaces;
using Client.Networking;
using Client.Services;
using Client.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Commands;

public static class CliHelpers
{
    public static IServiceProvider Services { get; private set; } = null!;

    private static void SetupServices()
    {
        ServiceCollection services = new();
        
        // Low-level infrastructure
        services.AddSingleton<IMessageCodec, JsonMessageCodec>();
        services.AddSingleton<ITransport, WebSocketTransport>();
        
        services.AddSingleton<IAppDataPaths, AppDataPaths>();
        services.AddSingleton<IFileStore, JsonFileStore>();

        // Protocol/session layer
        services.AddSingleton<IServerSession, ServerSession>();
        
        // Storage
        services.AddSingleton<IUserStore, UserStore>();
        services.AddSingleton<ILocalSavesStore, LocalSavesStore>();
        services.AddSingleton<ISettingsStore, SettingsStore>();

        // App services
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ILoginCoordinator, LoginCoordinator>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<IAppStartupService, AppStartupService>();
        services.AddSingleton<ISaveCatalogService, SaveCatalogService>();
        services.AddSingleton<ISaveSyncService, SaveSyncService>();
        services.AddSingleton<ISelectSaveForUploadService, SelectSaveForUploadService>();
        services.AddSingleton<ISelectSaveForDownloadService, SelectSaveForDownloadService>();
        services.AddSingleton<IFileSystemPickerService, FileSystemPickerService>();
        services.AddSingleton<ICreateUserService, CliCreateUserService>();
        services.AddSingleton<IModalService, CliModalService>();
        services.AddSingleton<IOpenSettingsService, OpenSettingsService>();
        services.AddSingleton<IServerStatusService, ServerStatusService>();
        services.AddSingleton<INoConnectionHandlerService, NoConnectionHandlerService>();
        services.AddSingleton<IInitialSetupService, CliInitialSetupService>();
        services.AddSingleton<IErrorPresenter, ErrorPresenter>();
        services.AddSingleton<ITaskRunner, TaskRunner>();
        
        Services = services.BuildServiceProvider();
    }
    
    public static async Task Setup(CancellationToken ct = default)
    {
        SetupServices();
        
        Console.WriteLine("Logging you in... ");
        
        try
        {
            IAppStartupService startupService =
                Services.GetRequiredService<IAppStartupService>();

            ITaskRunner taskRunner = Services.GetRequiredService<ITaskRunner>();

            await taskRunner.RunAsync(startupService.StartAsync, ct);
        
            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed!");
            await Console.Error.WriteLineAsync(ex.ToString());

            throw;
        }
    }
}