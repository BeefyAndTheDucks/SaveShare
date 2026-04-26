using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public sealed class GetSaveInfoMessageHandler : MessageHandler<C2SGetSaveInfoMessage>
{
    protected override async Task<bool> Handle(C2SGetSaveInfoMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        Result<SaveInfo> getSaveInfoResult = await SaveRegistry.GetSaveInfo(message.SaveId, cancellationToken);
        if (!getSaveInfoResult.Succeeded)
        {
            await Error(ErrorCode.SaveDoesNotExist, getSaveInfoResult.Error, webSocket, cancellationToken);
            return false;
        }
        await MessageHelpers.SendMessage(new S2CGotSaveInfoMessage(getSaveInfoResult.Value), webSocket, cancellationToken);
        return false;
    }
}