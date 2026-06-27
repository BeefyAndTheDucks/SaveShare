using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Interfaces;

namespace Client.Services;

public sealed class ErrorPresenter(IModalService modalService) : IErrorPresenter
{
    public async Task ShowErrorAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        (string title, string message) = exception switch
        {
            ServerErrorException serverError =>
                ("Server Error", serverError.Message),

            UnexpectedServerMessageException unexpected =>
                ("Unexpected Server Response", unexpected.Message),

            SaveNotFoundException saveNotFound =>
                ("Save Not Found", saveNotFound.Message),
            
            TransportException transport =>
                ("Network Error", transport.Message),

            _ =>
                ("Unexpected Error", exception.Message)
        };
        
        await modalService.ShowAsync(
            title,
            message,
            yes: "Ok",
            no: null,
            cancellationToken);
    }
}