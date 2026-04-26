using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class DownloadSaveMessageHandler : MessageHandler<C2SDownloadSaveMessage>
{
    protected override async Task<bool> Handle(C2SDownloadSaveMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        User user = Program.ConnectionManagerV1.GetUser(webSocket);
        Result<bool> hasCheckoutResult = await SaveRegistry.HasCheckout(message.SaveId, user.Username, cancellationToken);
        
        if (!hasCheckoutResult.Succeeded)
        {
            await Error(ErrorCode.FailedToDownload, hasCheckoutResult.Error, webSocket, cancellationToken);
            return false;
        }
        if (!hasCheckoutResult.Value)
        {
            await Error(ErrorCode.FailedToDownload, "You haven't checked out the save, please check out the save first.", webSocket, cancellationToken);
            return false;
        }

        long byteCount = await DirectoryPacker.GetPackedSize(SaveRegistry.GetRealSavePath(message.SaveId), cancellationToken);
        await MessageHelpers.SendMessage(new S2CReadyToSendBinaryDataMessage(byteCount), webSocket, cancellationToken);
        await MessageHelpers.AwaitResponse<C2SReadyForBinaryDataMessage>(webSocket, cancellationToken);

        WebSocketStream stream = WebSocketStream.CreateWritableMessageStream(webSocket, WebSocketMessageType.Binary);
        await DirectoryPacker.PackDirectory(SaveRegistry.GetRealSavePath(message.SaveId), stream, cancellationToken);
        await stream.DisposeAsync();
        return false;
    }
}