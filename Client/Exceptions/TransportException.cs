using System;

namespace Client.Exceptions;

public class TransportException(string message, Exception? innerException = null)
    : Exception(message, innerException);