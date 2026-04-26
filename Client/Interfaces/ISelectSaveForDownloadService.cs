using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Client.Interfaces;

public interface ISelectSaveForDownloadService
{
    Task<SelectSaveForDownloadResult?> ShowAsync(CancellationToken cancellationToken = default);
}

public sealed record SelectSaveForDownloadResult(string TargetPath, SaveInfo SaveInfo);