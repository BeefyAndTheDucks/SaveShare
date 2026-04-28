using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class UploadSaveChangesMessageHandler : MessageHandler<C2SUploadSaveChangesMessage>
{
    protected override async Task<bool> Handle(C2SUploadSaveChangesMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        if (!SaveRegistry.SaveExists(message.SaveId))
        {
            await Error(ErrorCode.SaveDoesNotExist, "Save does not exist", webSocket, cancellationToken);
            return false;
        }
        
        User user = Program.ConnectionManagerV1.GetUser(webSocket);
        Result<bool> hasCheckoutResult = await SaveRegistry.HasCheckout(message.SaveId, user.Username, cancellationToken);
        
        if (hasCheckoutResult.Failed)
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
        if (getPathResult.Failed)
        {
            await Error(ErrorCode.SaveFilesMissing, getPathResult.Error, webSocket, cancellationToken);
            return false;
        }
        
        IProgress<double> progress = new MessageHelpers.MessageProgress(webSocket, cancellationToken);
        await DirectoryPacker.BuildAndPackSignaturesAsync(getPathResult.Value, () => WebSocketStream.Create(webSocket, WebSocketMessageType.Binary), progress,
            async (byteSize, token) =>
            {
                await MessageHelpers.SendMessage(new S2CReadyToSendBinaryDataMessage(byteSize), webSocket, token);
                Result<C2SReadyForBinaryDataMessage> awaitResult = await MessageHelpers.AwaitResponse<C2SReadyForBinaryDataMessage>(webSocket, token);
                if (awaitResult.Failed)
                {
                    await Error(ErrorCode.UnexpectedResponse, awaitResult.Error, webSocket, token);
                    throw new Exception(awaitResult.Error);
                }
            },
            true, cancellationToken);

        Result<C2SReadyToSendBinaryDataMessage> awaitResult = await MessageHelpers.AwaitResponse<C2SReadyToSendBinaryDataMessage>(webSocket, cancellationToken);
        if (awaitResult.Failed)
        {
            await Error(ErrorCode.UnexpectedResponse, awaitResult.Error, webSocket, cancellationToken);
            throw new Exception(awaitResult.Error);
        }
        await MessageHelpers.SendMessage(new S2CReadyForBinaryDataMessage(), webSocket, cancellationToken);
        await using (Stream stream = WebSocketStream.Create(webSocket, WebSocketMessageType.Binary))
        {
            await DirectoryPacker.ApplyDeltasAsync(getPathResult.Value, stream, progress, cancellationToken);
        }
        await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully updated save data"), webSocket, cancellationToken);
        
        Result updateResult = await SaveRegistry.UpdateSaveInfo(message.SaveId, info =>
        {
            info.LastSyncedByUserName = Program.ConnectionManagerV1.GetUser(webSocket).Username;
            info.LastSyncedAt = DateTime.UtcNow;
            return info;
        }, cancellationToken);
        
        if (updateResult.Failed)
        {
            await Error(ErrorCode.FailedToDownload, updateResult.Error, webSocket, cancellationToken);
            return false;
        }
        
        return true;
    }
}