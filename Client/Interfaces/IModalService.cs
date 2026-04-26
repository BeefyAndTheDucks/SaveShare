using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IModalService
{
    Task<bool> ShowAsync(string title, string message, string yes, string? no, CancellationToken cancellationToken = default);
}