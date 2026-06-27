using Client.Interfaces;
using Common;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Extensions;

public static class SaveInfoExtensions
{
    extension(SaveInfo saveInfo)
    {
        public bool IsCheckedOutByLocalUser()
        {
            IAuthenticationService authService = App.Services.GetRequiredService<IAuthenticationService>();
            
            return saveInfo.CheckedOutByUserName == authService.CurrentUser?.Username;
        }
    }
}