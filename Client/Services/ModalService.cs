using System.Threading;
using System.Threading.Tasks;
using Client.Dialogs;
using Client.Interfaces;

namespace Client.Services;

public class ModalService(IMainWindowProvider mainWindowProvider) : IModalService
{
    public async Task<bool> ShowAsync(string title, string message, string yes, string? no, CancellationToken cancellationToken = default)
    {
        ModalDialog dialog = new(title, message, yes, no);
        
        bool result = await dialog.ShowDialog<bool>(mainWindowProvider.MainWindow);

        return result;
    }
}