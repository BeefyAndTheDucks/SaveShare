using System;
using Common;

namespace Client.Exceptions;

public sealed class ServerErrorException(S2CErrorMessage error) : Exception(error.Message)
{
    public S2CErrorMessage Error { get; } = error;
}