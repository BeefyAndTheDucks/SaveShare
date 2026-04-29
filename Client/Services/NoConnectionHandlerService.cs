using Client.Dialogs;
using Client.Interfaces;

namespace Client.Services;

public class NoConnectionHandlerService(IMainWindowProvider mainWindowProvider) : INoConnectionHandlerService
{
    public void CheckNoConnection()
    {
        new NoConnectionDialog().ShowDialog(mainWindowProvider.MainWindow);
    }
}