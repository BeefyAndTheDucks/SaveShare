using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IFileStore
{
    Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken = default);
    Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    
    bool Exists(string path);
}