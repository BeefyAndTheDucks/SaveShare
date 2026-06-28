using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class ReleaseMessageHandler : MessageHandler<C2SReleaseMessage>
{
    protected override async Task Handle(C2SReleaseMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        User user = Program.ConnectionManagerV1.GetUser(webSocket);
        Result result = await SaveRegistry.Release(message.SaveId, user.Username, cancellationToken);

        if (result.Succeeded)
        {
            await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully released save"), webSocket, cancellationToken);
            return;
        }

        await Error(ErrorCode.ReleaseFailed, result.Error, webSocket, cancellationToken);
    }
}