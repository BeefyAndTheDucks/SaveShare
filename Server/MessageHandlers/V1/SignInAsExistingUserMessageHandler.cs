using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class SignInAsExistingUserMessageHandler : MessageHandler<C2SSignInAsExistingUserMessage>
{
    protected override async Task<bool> Handle(C2SSignInAsExistingUserMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        await Error(ErrorCode.AlreadySignedIn, "You are already signed in.", webSocket, cancellationToken);
        return false;
    }
}