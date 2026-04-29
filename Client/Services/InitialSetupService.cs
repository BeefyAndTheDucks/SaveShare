using System.Threading;
using System.Threading.Tasks;
using Client.Dialogs;
using Client.Interfaces;

namespace Client.Services;

public class InitialSetupService(IMainWindowProvider mainWindowProvider) : IInitialSetupService
{
    public async Task<SetupResult?> ShowAsync(string? error, CancellationToken cancellationToken = default)
    {
        InitialSetupWindow dialog = new(error);
        
        InitialSetupWindow.Result result = await dialog.ShowDialog<InitialSetupWindow.Result>(mainWindowProvider.MainWindow);
        if (result is not { Valid: true })
        {
            App.Close();
            return null;
        }
        
        return new SetupResult(result.ServerUri!);
    }
}