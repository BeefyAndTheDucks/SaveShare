using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IFileSystemPickerService
{
    Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken = default);
    Task<string?> SaveFileAsync(string title, string? defaultName, string? defaultExtension, CancellationToken cancellationToken = default);
    Task<string?> OpenFileAsync(string title, CancellationToken cancellationToken = default);
}