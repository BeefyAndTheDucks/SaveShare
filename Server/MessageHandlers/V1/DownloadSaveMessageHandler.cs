using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class DownloadSaveMessageHandler : MessageHandler<C2SDownloadSaveMessage>
{
    protected override async Task Handle(C2SDownloadSaveMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        User user = Program.ConnectionManagerV1.GetUser(webSocket);
        Result<bool> hasCheckoutResult = await SaveRegistry.HasCheckout(message.SaveId, user.Username, cancellationToken);
        
        if (!hasCheckoutResult.Succeeded)
        {
            await Error(ErrorCode.FailedToDownload, hasCheckoutResult.Error, webSocket, cancellationToken);
            return;
        }
        if (!hasCheckoutResult.Value)
        {
            await Error(ErrorCode.NotCheckedOut, "You haven't checked out the save, please check out the save first.", webSocket, cancellationToken);
            return;
        }
        
        Result<string> getPathResult = await SaveRegistry.GetRealSavePath(message.SaveId, cancellationToken);
        if (!getPathResult.Succeeded)
        {
            await Error(ErrorCode.SaveFilesMissing, getPathResult.Error, webSocket, cancellationToken);
            return;
        }

        await SavePacker.PackAsync(getPathResult.Value, () => WebSocketStream.Create(webSocket, WebSocketMessageType.Binary), true,
            async (size, token) =>
            {
                await MessageHelpers.SendMessage(new S2CReadyToSendBinaryDataMessage(size), webSocket, token);
                await MessageHelpers.AwaitResponse<C2SReadyForBinaryDataMessage>(webSocket, token);
            }, cancellationToken);
    }
}