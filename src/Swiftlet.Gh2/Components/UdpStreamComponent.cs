using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.UdpStream)]
public sealed class UdpStreamComponent : Component
{
    public UdpStreamComponent() : base(new Nomen("UDP Stream", "Send data as a UDP datagram to a specified host and port", "Swiftlet", "3. Send")) { }
    public UdpStreamComponent(IReader reader) : base(reader) { }
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;
    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Host", "H", "Target host (IP address or hostname)");
        inputs.AddInteger("Port", "P", "Target port number (0-65535)");
        var data = new ByteArrayParameter("Data", "D", "Data to send as a byte array") { Requirement = Requirement.MayBeMissing };
        data.Set(new[] { Array.Empty<byte>() });
        inputs.Add(data);
    }
    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddBoolean("Success", "S", "True if the datagram was sent successfully");
        outputs.AddInteger("Bytes Sent", "B", "Number of bytes sent");
    }
    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string host); access.GetItem(1, out int port);
        access.GetItem(2, out byte[] data);
        var result = SendUtilities.SendUdpAsync(host, port, data ?? Array.Empty<byte>(), access.Solution.Token).GetAwaiter().GetResult();
        if (result.Error is not null)
        {
            access.AddError("UDP", result.Error);
            if (result.IsValidationError) return;
        }
        if (result.Warning is not null) access.AddWarning("UDP", result.Warning);
        access.SetItem(0, result.Success); access.SetItem(1, result.BytesSent);
    }
}
