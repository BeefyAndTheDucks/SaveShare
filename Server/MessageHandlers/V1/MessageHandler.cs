using System.Net.WebSockets;
using Common;
using Newtonsoft.Json.Linq;

namespace Server.MessageHandlers.V1;

public record HandleResult(bool Handled, bool PropagateUpdates);

public abstract class MessageHandler
{
    public virtual async Task<HandleResult> Handle(JObject messageJsonObject, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        C2SMessage message = messageJsonObject.ParseAsMessage();

        return await Handle(message, webSocket, cancellationToken);
    }
    
    protected abstract Task<HandleResult> Handle(C2SMessage? message, WebSocket webSocket, CancellationToken cancellationToken = default);
}

public abstract class MessageHandler<TMessage> : MessageHandler where TMessage : C2SMessage
{
    protected async Task Error(ErrorCode errorCode, string errorMessage, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        await MessageHelpers.SendMessage(new S2CErrorMessage(errorCode, errorMessage), webSocket, cancellationToken);
    }
    
    public override async Task<HandleResult> Handle(JObject messageJsonObject, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        Result<TMessage> parseResult = messageJsonObject.TryParseAsMessage<TMessage>();

        if (!parseResult.Succeeded) return new HandleResult(false, false);

        bool propagate = await Handle(parseResult.Value, webSocket, cancellationToken);

        return new HandleResult(true, propagate);
    }

    protected override Task<HandleResult> Handle(C2SMessage? message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This method should not be called");
    }

    protected abstract Task<bool> Handle(TMessage message, WebSocket webSocket, CancellationToken cancellationToken = default);
}