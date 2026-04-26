using System.Net.WebSockets;
using Common;

namespace Server.MessageHandlers.V1;

public class CheckoutSaveMessageHandler : MessageHandler<C2SCheckoutSaveMessage>
{
    protected override async Task<bool> Handle(C2SCheckoutSaveMessage message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        User user = Program.ConnectionManagerV1.GetUser(webSocket);
        
        Result checkoutResult = await SaveRegistry.TryCheckout(message.SaveId, user.Username, cancellationToken);

        if (!checkoutResult.Succeeded)
        {
            await Error(ErrorCode.FailedToCheckOut, checkoutResult.Error, webSocket, cancellationToken);
            return false;
        }

        await MessageHelpers.SendMessage(new S2CSuccessMessage("Successfully checked out"), webSocket, cancellationToken);
        return true;
    }
}