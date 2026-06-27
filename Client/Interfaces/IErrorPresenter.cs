using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Interfaces;

public interface IErrorPresenter
{
    Task ShowErrorAsync(Exception exception, CancellationToken cancellationToken = default);
}