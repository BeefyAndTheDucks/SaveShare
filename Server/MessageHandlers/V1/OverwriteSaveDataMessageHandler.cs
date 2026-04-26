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
            await Error(ErrorCode.OverwriteSaveDataFailed, "Save does not exist", webSocket, cancellationToken);
            return false;
        }
        
        string path = SaveRegistry.GetRealSavePath(message.SaveId);
        Directory.CreateDirectory(path);
        await MessageHelpers.SendMessage(new S2CReadyForBinaryDataMessage(), webSocket, cancellationToken);
        WebSocketStream stream = WebSocketStream.CreateReadableMessageStream(webSocket);
        await DirectoryPacker.UnpackDirectory(stream, path, cancellationToken);
        await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully overwrote the old save data (if any)"), webSocket, cancellationToken);

        return false; // Don't propagate updates
    }
}
