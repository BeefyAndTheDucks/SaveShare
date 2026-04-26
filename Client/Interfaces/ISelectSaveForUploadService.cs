using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface ISelectSaveForUploadService
{
    Task<SelectSaveForUploadResult?> ShowAsync(CancellationToken cancellationToken = default);
}

public sealed record SelectSaveForUploadResult(string SavePath, string SaveName);