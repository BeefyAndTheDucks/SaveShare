using System.Net.WebSockets;
using Common;
using Common.Protocol.V1;
using JetBrains.Annotations;

namespace Server.MessageHandlers.V1;

[UsedImplicitly]
public class RegisterNewSaveMessageHandler : MessageHandler<C2SRegisterNewSaveMessage>
{
    protected override async Task Handle(C2SRegisterNewSaveMessage message, WebSocket webSocket,
        CancellationToken cancellationToken = default)
    {
        Result<SaveId> createSaveResult = await SaveRegistry.CreateSave(cancellationToken);

        if (!createSaveResult.Succeeded)
        {
            await Error(ErrorCode.FailedToCreateNewSave, createSaveResult.Error, webSocket, cancellationToken);
            return;
        }
        
        Result updateSaveResult = await SaveRegistry.UpdateSaveInfo(createSaveResult.Value, info =>
        {
            info.Name = message.Name;
            return info;
        }, cancellationToken);
        if (!updateSaveResult.Succeeded)
        {
            await Error(ErrorCode.FailedToCreateNewSave, updateSaveResult.Error, webSocket, cancellationToken);
            return;
        }
        
        Result<SaveInfo> getSaveResult = await SaveRegistry.GetSaveInfo(createSaveResult.Value, cancellationToken);
        if (!getSaveResult.Succeeded)
        {
            await Error(ErrorCode.FailedToCreateNewSave, getSaveResult.Error, webSocket, cancellationToken);
            return;
        }

        await MessageHelpers.SendMessage(new S2CRegisteredNewSaveMessage(getSaveResult.Value), webSocket, cancellationToken);
    }
}