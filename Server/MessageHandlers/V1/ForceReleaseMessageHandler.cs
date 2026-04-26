using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class ForceReleaseMessageHandler : MessageHandler<C2SForceReleaseMessage>
{
    protected override async Task<bool> Handle(C2SForceReleaseMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        Result result = await SaveRegistry.ForceRelease(message.SaveId, cancellationToken);

        if (result.Succeeded)
        {
            await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully force-released save"), webSocket, cancellationToken);
            return true;
        }

        await Error(ErrorCode.ForceReleaseFailed, result.Error, webSocket, cancellationToken);
        return false;
    }
}