using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class CheckoutSaveMessageHandler : MessageHandler<C2SCheckoutSaveMessage>
{
    protected override async Task Handle(C2SCheckoutSaveMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        User user = Program.ConnectionManagerV1.GetUser(webSocket);
        
        Result checkoutResult = await SaveRegistry.TryCheckout(message.SaveId, user.Username, cancellationToken);

        if (!checkoutResult.Succeeded)
        {
            await Error(ErrorCode.FailedToCheckOut, checkoutResult.Error, webSocket, cancellationToken);
            return;
        }

        await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully checked out"), webSocket, cancellationToken);
    }
}