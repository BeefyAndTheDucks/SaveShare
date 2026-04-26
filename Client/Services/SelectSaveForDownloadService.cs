using System.Threading;
using System.Threading.Tasks;
using Client.Dialogs;
using Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Services;

public sealed class SelectSaveForDownloadService(IMainWindowProvider mainWindowProvider) : ISelectSaveForDownloadService
{
    public async Task<SelectSaveForDownloadResult?> ShowAsync(CancellationToken cancellationToken = default)
    {
        AddCloudSaveDialog dialog = App.Services.GetRequiredService<AddCloudSaveDialog>();
        
        AddCloudSaveDialog.Result result = await dialog.ShowDialog<AddCloudSaveDialog.Result>(mainWindowProvider.MainWindow);
        if (result is not { Valid: true })
            return null;
        
        return new SelectSaveForDownloadResult(result.TargetPath!, result.SaveInfo!.Value);
    }
}