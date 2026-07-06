using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class DownloadSaveChangesMessageHandler : MessageHandler<C2SDownloadSaveChangesMessage>
{
    protected override async Task Handle(C2SDownloadSaveChangesMessage message, WebSocket webSocket,
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
        
        SaveManifest serverManifest = await SaveManifest.From(getPathResult.Value, new MessageHelpers.MessageProgress(webSocket, cancellationToken), cancellationToken);
        await MessageHelpers.SendMessage(new S2CSaveManifestMessage(serverManifest), webSocket, cancellationToken);
        await MessageHelpers.SendMessage(new S2CReadyForBinaryDataMessage(), webSocket, cancellationToken);
        await using Stream stream = WebSocketStream.Create(webSocket, WebSocketMessageType.Binary);
        IProgress<double> progress = new MessageHelpers.MessageProgress(webSocket, cancellationToken);
        await SavePacker.CreateDeltasAsync(getPathResult.Value, stream, stream, serverManifest, message.ClientSideManifest, progress, async (byteSize, ct) =>
        {
            await MessageHelpers.SendMessage(new S2CReadyToSendBinaryDataMessage(byteSize), webSocket, ct);
            await MessageHelpers.AwaitResponse<C2SReadyForBinaryDataMessage>(webSocket, ct);
        }, cancellationToken);
    }
}