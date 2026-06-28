using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class ListSavesMessageHandler : MessageHandler<C2SListSavesMessage>
{
    protected override async Task Handle(C2SListSavesMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        SaveInfo[] saves = await SaveRegistry.GetSaves(cancellationToken);
        S2CSaveListMessage saveListMessage = new(saves);
        await MessageHelpers.SendMessage(saveListMessage, webSocket, cancellationToken);
    }
}