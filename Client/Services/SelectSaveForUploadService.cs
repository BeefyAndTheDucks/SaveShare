using System.Threading;
using System.Threading.Tasks;
using Client.Dialogs;
using Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Services;

public sealed class SelectSaveForUploadService(IMainWindowProvider mainWindowProvider) : ISelectSaveForUploadService
{
    public async Task<SelectSaveForUploadResult?> ShowAsync(CancellationToken cancellationToken = default)
    {
        AddLocalSaveDialog dialog = App.Services.GetRequiredService<AddLocalSaveDialog>();
        
        AddLocalSaveDialog.Result result = await dialog.ShowDialog<AddLocalSaveDialog.Result>(mainWindowProvider.MainWindow);
        if (result is not { Valid: true })
            return null;
        
        return new SelectSaveForUploadResult(result.SavePath!, result.SaveName!);
    }
}