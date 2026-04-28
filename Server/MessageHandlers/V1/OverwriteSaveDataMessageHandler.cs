using System.Net.WebSockets;
using Common;
using WebSocketStream = System.Net.WebSockets.WebSocketStream;

namespace Server.MessageHandlers.V1;

public class OverwriteSaveDataMessageHandler : MessageHandler<C2SOverwriteSaveDataMessage>
{
    protected override async Task<bool> Handle(C2SOverwriteSaveDataMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        if (!SaveRegistry.SaveExists(message.SaveId))
        {
            await Error(ErrorCode.SaveDoesNotExist, "Save does not exist", webSocket, cancellationToken);
            return false;
        }
        
        string path = SaveRegistry.GetRealSavePathNoExistsCheck(message.SaveId);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        await MessageHelpers.SendMessage(new S2CReadyForBinaryDataMessage(), webSocket, cancellationToken);
        await using Stream stream = WebSocketStream.Create(webSocket, WebSocketMessageType.Binary);
        await DirectoryPacker.UnpackDirectoryAsync(stream, path, cancellationToken);
        await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully overwrote the old save data (if any)"), webSocket, cancellationToken);
        
        Result updateResult = await SaveRegistry.UpdateSaveInfo(message.SaveId, info =>
        {
            info.LastSyncedByUserName = Program.ConnectionManagerV1.GetUser(webSocket).Username;
            info.LastSyncedAt = DateTime.UtcNow;
            return info;
        }, cancellationToken);

        if (updateResult.Failed)
        {
            await Error(ErrorCode.OverwriteSaveDataFailed, "Failed to update save data", webSocket, cancellationToken);
            return false;
        }

        return true;
    }
}
