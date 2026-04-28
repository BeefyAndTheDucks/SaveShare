using Avalonia;
using System;
using System.CommandLine;
using System.Threading.Tasks;
using Client.Commands;
using Client.Interfaces;
using Client.Networking;
using Client.ViewModels;
using Client.Views;
using Common;

namespace Client;

sealed class Program
{
    public static ConnectionManager ConnectionManager { get; private set; } = null!;
    public static MainWindowViewModel? ViewModel { get; set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        RootCommand rootCommand = new("SaveShare");

        CliCommand cliCommand = new CliCommand();
        rootCommand.Add(cliCommand.CreateCommand());

        rootCommand.SetAction(_ =>
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        });

        ParseResult parseResult = rootCommand.Parse(args);
        parseResult.Invoke();
    } 

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .UseSkia()
            .With(new X11PlatformOptions
            {
                WmClass = "SaveShareClientGUI",
            })
            .LogToTrace();
}