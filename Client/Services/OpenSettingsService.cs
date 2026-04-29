using Client.Dialogs;
using Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Services;

public class OpenSettingsService(IMainWindowProvider mainWindowProvider) : IOpenSettingsService
{
    public void OpenSettings()
    {
        App.Services.GetRequiredService<SettingsDialog>().ShowDialog(mainWindowProvider.MainWindow);
    }
}