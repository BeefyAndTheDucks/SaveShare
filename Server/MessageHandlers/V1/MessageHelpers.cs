using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Common;
using Common.Protocol;
using Common.Protocol.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Server.MessageHandlers.V1;

public static class MessageHandlerFactory
{
    private static MessageHandler[] MessageHandlers
    {
        get
        {
            if (field is not null)
                return field;
            
            Stopwatch sw = Stopwatch.StartNew();
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            field = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => typeof(MessageHandler).IsAssignableFrom(t))
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Select(t => Activator.CreateInstance(t) as MessageHandler)
                .Where(inst => inst != null)
                .ToArray()!;
            
            Console.WriteLine($"Loaded {field.Length} message handlers in {sw.ElapsedMilliseconds}ms");
            
            return field;
        }
    }
    
    public static async Task Handle(JObject messageJObject, WebSocket ws, CancellationToken ct = default)
    {
        foreach (MessageHandler messageHandler in MessageHandlers)
        {
            HandleResult result = await messageHandler.Handle(messageJObject, ws, ct);
            if (result.Handled)
                return;
        }
        await HandleUnknownMessage(messageJObject, ws, ct);
    }

    private static async Task HandleUnknownMessage(JObject message, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        string messageRawJson = message.ToString();
        S2CErrorMessage response = new(ErrorCode.UnknownMessage, $"Unknown or unhandled message type (Raw JSON: {messageRawJson})");
        await MessageHelpers.SendMessage(response, webSocket, cancellationToken);
    }
}

public static class MessageHelpers
{
    private static readonly IReadOnlyDictionary<C2SMessageType, Type> ClientMessageTypes =
        MessageTypeHelpers.BuildMessageTypeMap<C2SMessage, C2SMessageTypeAttribute, C2SMessageType>(attr => attr.Type);
    
    private static readonly SemaphoreSlim SendSemaphore = new(1, 1);
    
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
        await SendSemaphore.WaitAsync(ct);
        try
        {
            string json = JsonConvert.SerializeObject(message);
            await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, ct);
        }
        finally
        {
            SendSemaphore.Release();
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