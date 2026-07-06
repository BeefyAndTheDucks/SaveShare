using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Client.Interfaces;

namespace Client.Services;

public sealed class FileSystemPickerService(IMainWindowProvider mainWindowProvider) : IFileSystemPickerService
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
        
        return folder?.TryGetLocalPath();
    }

    public async Task<string?> SaveFileAsync(string title, string? defaultName, string? defaultExtension,
        CancellationToken cancellationToken = default)
    {
        IStorageFile? file = await mainWindowProvider.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = defaultExtension,
            SuggestedFileName = defaultName
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> OpenFileAsync(string title, CancellationToken cancellationToken = default)
    {
        var files = await mainWindowProvider.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
        });
        
        return files.FirstOrDefault()?.TryGetLocalPath();
    }
}