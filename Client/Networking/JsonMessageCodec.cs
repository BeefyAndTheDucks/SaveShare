using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Client.Interfaces;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client.Networking;

public sealed class JsonMessageCodec : IMessageCodec
{
    private static readonly IReadOnlyDictionary<S2CMessageType, Type> ServerMessageTypes =
        MessageTypeHelpers.BuildMessageTypeMap<S2CMessage, S2CMessageTypeAttribute, S2CMessageType>(attr => attr.Type);
    
    public string Serialize(C2SMessage message)
    {
        return JsonConvert.SerializeObject(message);
    }

    public S2CMessage Deserialize(string json)
    {
        JObject obj = JObject.Parse(json);
        
        S2CMessageType messageType = ReadServerMessageType(obj);
        
        if (!ServerMessageTypes.TryGetValue(messageType, out Type? concreteType))
            throw new InvalidOperationException(
                $"No server message class is registered for message type '{messageType}'.");

        S2CMessage? message = (S2CMessage?)obj.ToObject(concreteType);

        if (message is null)
            throw new InvalidOperationException(
                $"Failed to deserialize server message of type '{messageType}'.");

        return message;
    }
    
    private static S2CMessageType ReadServerMessageType(JObject obj)
    {
        JToken? token = obj["Type"];

        if (token is null)
            throw new InvalidOperationException("Server message is missing required 'Type' property.");

        switch (token.Type)
        {
            case JTokenType.Integer:
            {
                int value = token.Value<int>();

                if (!Enum.IsDefined(typeof(S2CMessageType), value))
                    throw new InvalidOperationException($"Unknown server message type value '{value}'.");

                return (S2CMessageType)value;
            }
            case JTokenType.String:
            {
                string? value = token.Value<string>();

                if (!Enum.TryParse(value, ignoreCase: true, out S2CMessageType messageType))
                    throw new InvalidOperationException($"Unknown server message type value '{value}'.");

                return messageType;
            }
            default:
                throw new InvalidOperationException(
                    $"Server message 'Type' property must be a string or integer, got '{token.Type}'.");
        }
    }
}