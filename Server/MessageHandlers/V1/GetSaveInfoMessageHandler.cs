using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public sealed class GetSaveInfoMessageHandler : MessageHandler<C2SGetSaveInfoMessage>
{
    protected override async Task Handle(C2SGetSaveInfoMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        Result<SaveInfo> getSaveInfoResult = await SaveRegistry.GetSaveInfo(message.SaveId, cancellationToken);
        if (!getSaveInfoResult.Succeeded)
        {
            await Error(ErrorCode.SaveDoesNotExist, getSaveInfoResult.Error, webSocket, cancellationToken);
            return;
        }
        await MessageHelpers.SendMessage(new S2CGotSaveInfoMessage(getSaveInfoResult.Value), webSocket, cancellationToken);
    }
}