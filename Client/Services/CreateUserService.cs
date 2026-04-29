using System.Threading;
using System.Threading.Tasks;
using Client.Dialogs;
using Client.Interfaces;

namespace Client.Services;

public class CreateUserService(IMainWindowProvider mainWindowProvider) : ICreateUserService
{
    public async Task<CreateUserResult?> ShowAsync(string? error, CancellationToken cancellationToken = default)
    {
        CreateNewUserDialog dialog = new(error);
        
        CreateNewUserDialog.Result result = await dialog.ShowDialog<CreateNewUserDialog.Result>(mainWindowProvider.MainWindow);
        if (result is not { Valid: true })
        {
            App.Close();
            return null;
        }
        
        return new CreateUserResult(result.UserName!);
    }
}