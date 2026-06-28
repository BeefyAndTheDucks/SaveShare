using System.Net.WebSockets;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class SignInAsExistingUserMessageHandler : MessageHandler<C2SSignInAsExistingUserMessage>
{
    protected override async Task Handle(C2SSignInAsExistingUserMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        await Error(ErrorCode.AlreadySignedIn, "You are already signed in.", webSocket, cancellationToken);
    }
}