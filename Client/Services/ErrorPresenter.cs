using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Client.Exceptions;
using Client.Helpers;
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
            
            IncompatibleVersionsException incompatibleVersions =>
                ("Incompatible Versions", incompatibleVersions.Message),
            
            WebSocketException webSocket =>
                ("Network Error", webSocket.Message),
            
            TransportException transport =>
                ("Network Error", transport.Message),

            _ =>
                ($"Unexpected Error ({exception.GetType().Name}", exception.Message)
        };
        
        Console.Error.WriteLine(exception);
        
        NativeAudio.PlayAlertSound();
        
        await modalService.ShowAsync(
            title,
            title + ": " + message,
            yes: "Ok",
            no: null,
            cancellationToken);
    }
}