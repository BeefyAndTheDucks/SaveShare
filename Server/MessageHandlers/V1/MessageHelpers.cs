using System.Net.WebSockets;
using System.Text;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Server.MessageHandlers.V1;

public static class MessageHandlerFactory
{
    private static readonly List<MessageHandler> MessageHandlers =
    [
        new SignInAsNewUserMessageHandler(),
        new SignInAsExistingUserMessageHandler(),
        new ListSavesMessageHandler(),
        new ForceReleaseMessageHandler(),
        new ReleaseMessageHandler(),
        new RegisterNewSaveMessageHandler(),
        new OverwriteSaveDataMessageHandler(),
        new CheckoutSaveMessageHandler(),
        new DownloadSaveMessageHandler(),
        new DownloadSaveChangesMessageHandler(),
        new UploadSaveChangesMessageHandler(),
    ];
    
    public static async Task<bool> Handle(JObject messageJObject, WebSocket ws, CancellationToken ct = default)
    {
        foreach (MessageHandler messageHandler in MessageHandlers)
        {
            HandleResult result = await messageHandler.Handle(messageJObject, ws, ct);
            if (result.Handled)
                return result.PropagateUpdates;
        }
        await HandleUnknownMessage(ws, ct);
        return false;
    }

    private static async Task HandleUnknownMessage(WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        S2CErrorMessage response = new(ErrorCode.UnknownMessage, "Unknown message type");
        await MessageHelpers.SendMessage(response, webSocket, cancellationToken);
    }
}

public static class MessageHelpers
{
    private static readonly IReadOnlyDictionary<C2SMessageType, Type> ClientMessageTypes =
        MessageTypeHelpers.BuildMessageTypeMap<C2SMessage, C2SMessageTypeAttribute, C2SMessageType>(attr => attr.Type);
    
    private static SemaphoreSlim _sendSemaphore = new(1, 1);
    
    private static C2SMessageType ReadClientMessageType(JObject obj)
    {
        JToken? token = obj["Type"];

        if (token is null)
            throw new InvalidOperationException("Server message is missing required 'Type' property.");

        switch (token.Type)
        {
            case JTokenType.Integer:
            {
                int value = token.Value<int>();

                if (!Enum.IsDefined(typeof(C2SMessageType), value))
                    throw new InvalidOperationException($"Unknown server message type value '{value}'.");

                return (C2SMessageType)value;
            }
            case JTokenType.String:
            {
                string? value = token.Value<string>();

                if (!Enum.TryParse(value, ignoreCase: true, out C2SMessageType messageType))
                    throw new InvalidOperationException($"Unknown server message type value '{value}'.");

                return messageType;
            }
            default:
                throw new InvalidOperationException(
                    $"Server message 'Type' property must be a string or integer, got '{token.Type}'.");
        }
    }
    
    extension(JObject messageJson)
    {
        public C2SMessage ParseAsMessage()
        {
            C2SMessageType messageType = ReadClientMessageType(messageJson);
            
            if (!ClientMessageTypes.TryGetValue(messageType, out Type? concreteType))
                throw new InvalidOperationException(
                    $"No client message class is registered for message type '{messageType}'.");
            
            C2SMessage? message = (C2SMessage?)messageJson.ToObject(concreteType);
            
            if (message is null)
                throw new InvalidOperationException(
                    $"Failed to deserialize server message of type '{messageType}'.");
            
            return message;
        }

        public Result<TMessage> TryParseAsMessage<TMessage>() where TMessage : C2SMessage
        {
            C2SMessage message = messageJson.ParseAsMessage();
            if (message is TMessage typedMessage)
                return Result<TMessage>.Success(typedMessage);
            return Result<TMessage>.Failure($"Message is not of type '{typeof(TMessage)}'.");
        }

        public bool TryParseAsMessage<TMessage>(out TMessage? message) where TMessage : C2SMessage
        {
            Result<TMessage> res = messageJson.TryParseAsMessage<TMessage>();
            message = res.Value;
            return res.Succeeded;
        }
    }

    public static async Task SendMessage(S2CMessage message, WebSocket ws, CancellationToken ct = default)
    {
        await _sendSemaphore.WaitAsync(ct);
        try
        {
            string json = JsonConvert.SerializeObject(message);
            await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, ct);
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    public static async Task<JObject> AwaitJsonResponse(WebSocket ws, CancellationToken ct = default)
    {
        string rawJson = await WebSocketUtils.ReceiveString(ws, ct);
        return JObject.Parse(rawJson);
    }

    public static async Task<C2SMessage?> AwaitResponse(WebSocket ws, CancellationToken ct = default)
    {
        JObject messageJsonObject = await AwaitJsonResponse(ws, ct);
        return messageJsonObject.ParseAsMessage();
    }

    public static async Task<Result<TMessage>> AwaitResponse<TMessage>(WebSocket ws, CancellationToken ct = default) where TMessage : C2SMessage
    {
        JObject messageJsonObject = await AwaitJsonResponse(ws, ct);
        return messageJsonObject.TryParseAsMessage<TMessage>();
    }

    public class MessageProgress(WebSocket ws, CancellationToken ct = default) : IProgress<double>
    {
        public void Report(double value)
        {
            _ = SendMessage(new S2CProgressMessage(value), ws, ct);
        }
    }
}