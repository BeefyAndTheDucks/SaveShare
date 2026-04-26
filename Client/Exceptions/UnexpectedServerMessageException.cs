using System;
using Common;

namespace Client.Exceptions;

public class UnexpectedServerMessageException(S2CMessageType actualType)
    : Exception($"Unexpected server message type: {actualType}")
{
    public S2CMessageType ActualType { get; } = actualType;
}