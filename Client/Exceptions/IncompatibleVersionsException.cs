using System;

namespace Client.Exceptions;

public class IncompatibleVersionsException(int myProtocolVersion, int serverProtocolVersion, string myApplicationVersion, string serverApplicationVersion)
    : Exception($"Incompatible versions detected! You're using version {myApplicationVersion}, but the server is using version {serverApplicationVersion}. (Protocol version {myProtocolVersion} (client) vs {serverProtocolVersion} (server))");