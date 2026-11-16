namespace Swiftlet.Runtime;

/// <summary>
/// The one port rule for every Swiftlet listener (HTTP Listener, Server Input, WebSocket Server, UDP Listener,
/// MCP Server) in both shells. Port 0 is rejected: the TCP listeners run in a bridge that requires 1-65535, and
/// on UDP it would bind a random port the user cannot see.
/// </summary>
public static class ListenPorts
{
    public const int Min = 1;
    public const int Max = 65535;

    /// <summary>Validation message shown by the components.</summary>
    public const string Error = "Port must be between 1 and 65535";

    public static bool IsValid(int port) => port is >= Min and <= Max;
}
