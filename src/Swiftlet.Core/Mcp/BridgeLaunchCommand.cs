namespace Swiftlet.Core.Mcp;

public sealed class BridgeLaunchCommand
{
    public BridgeLaunchCommand(string command, IEnumerable<string>? args = null)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Args = args?.ToArray() ?? [];
    }

    public string Command { get; }

    public IReadOnlyList<string> Args { get; }
}
