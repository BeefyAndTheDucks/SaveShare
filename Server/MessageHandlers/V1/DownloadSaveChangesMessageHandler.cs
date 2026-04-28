using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class DownloadSaveChangesMessageHandler : MessageHandler<C2SDownloadSaveChangesMessage>
{
    protected override async Task<bool> Handle(C2SDownloadSaveChangesMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
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
            await Error(ErrorCode.NotCheckedOut, "You haven't checked out the save, please check out the save first.", webSocket, cancellationToken);
            return false;
        }
        
        Result<string> getPathResult = SaveRegistry.GetRealSavePath(message.SaveId);
        if (!getPathResult.Succeeded)
        {
            await Error(ErrorCode.SaveFilesMissing, getPathResult.Error, webSocket, cancellationToken);
            return false;
        }
            
        await MessageHelpers.SendMessage(new S2CReadyForBinaryDataMessage(), webSocket, cancellationToken);
        await using Stream stream = WebSocketStream.Create(webSocket, WebSocketMessageType.Binary);
        IProgress<double> progress = new MessageHelpers.MessageProgress(webSocket, cancellationToken);
        await DirectoryPacker.CreateDeltasAsync(getPathResult.Value, stream, stream, progress, async (byteSize, ct) =>
        {
            await MessageHelpers.SendMessage(new S2CReadyToSendBinaryDataMessage(byteSize), webSocket, ct);
        }, cancellationToken);

        return false;
    }
}