using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(string title, CancellationToken cancellationToken = default);
}