using System;
using Common.Protocol.V1;

namespace Client.Exceptions;

public class UnexpectedServerMessageException(S2CMessageType actualType)
    : Exception($"Unexpected server message type: {actualType}")
{
    public S2CMessageType ActualType { get; } = actualType;
}