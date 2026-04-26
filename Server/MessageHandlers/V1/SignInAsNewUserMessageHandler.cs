using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class SignInAsNewUserMessageHandler : MessageHandler<C2SSignInAsNewUserMessage>
{
    protected override async Task<bool> Handle(C2SSignInAsNewUserMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        await Error(ErrorCode.AlreadySignedIn, "You are already signed in.", webSocket, cancellationToken);
        return false;
    }
}