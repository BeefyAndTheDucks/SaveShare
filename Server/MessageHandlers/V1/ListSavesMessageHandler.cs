using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class ListSavesMessageHandler : MessageHandler<C2SListSavesMessage>
{
    protected override async Task<bool> Handle(C2SListSavesMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        SaveInfo[] saves = await SaveRegistry.GetSaves(cancellationToken);
        S2CSaveListMessage saveListMessage = new(saves);
        await MessageHelpers.SendMessage(saveListMessage, webSocket, cancellationToken);

        return false;
    }
}