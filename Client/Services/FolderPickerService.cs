using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Client.Interfaces;

namespace Client.Services;

public sealed class FolderPickerService(IMainWindowProvider mainWindowProvider) : IFolderPickerService
{
    public async Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IStorageFolder> folders =
            await mainWindowProvider.MainWindow.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false
                });

        IStorageFolder? folder = folders.FirstOrDefault();
        
        return folder?.Path.AbsolutePath;
    }
}