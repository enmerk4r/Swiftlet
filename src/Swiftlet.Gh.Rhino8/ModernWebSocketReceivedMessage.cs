namespace Swiftlet.Gh.Rhino8;

public sealed class ModernWebSocketReceivedMessage
{
    public ModernWebSocketReceivedMessage(ModernWebSocketConnection connection, string message)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Message = message ?? string.Empty;
        ReceivedAtUtc = DateTime.UtcNow;
    }

    public ModernWebSocketConnection Connection { get; }

    public string Message { get; }

    public DateTime ReceivedAtUtc { get; }
}
