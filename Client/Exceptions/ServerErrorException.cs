using System;
using Common.Protocol.V1;

namespace Client.Exceptions;

public sealed class ServerErrorException(S2CErrorMessage error) : Exception($"({error.Code}) {error.Message}")
{
    public S2CErrorMessage Error { get; } = error;
}