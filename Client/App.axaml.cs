using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Client.Dialogs;
using Client.Interfaces;
using Client.Networking;
using Client.Services;
using Client.Storage;
using Client.ViewModels;
using Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Client;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        
        ConfigureServices(services);
        
        Services = services.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();

        _ = StartApplicationAsync();
    }

    private static async void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            switch (Services)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }
    }

    private static async Task StartApplicationAsync()
    {
        try
        {
            IAppStartupService startupService =
                Services.GetRequiredService<IAppStartupService>();

            await startupService.StartAsync();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
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
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<ICreateUserService, CreateUserService>();
        services.AddSingleton<IModalService, ModalService>();
        
        // ViewModels
        services.AddTransient<MainWindowViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<AddCloudSaveDialog>();
        services.AddTransient<AddLocalSaveDialog>();
        
        // Providers
        services.AddSingleton<IMainWindowProvider, MainWindowProvider>();
    }
}