using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UdpStreamComponent : GH_Component
{
    public UdpStreamComponent()
        : base("UDP Stream", "UDP", "Send data as a UDP datagram to a specified host and port", ShellNaming.Category, ShellNaming.Send)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Host", "H", "Target host (IP address or hostname)", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Port", "P", "Target port number (0-65535)", GH_ParamAccess.item);
        pManager.AddParameter(new ByteArrayParam(), "Data", "D", "Data to send as a byte array", GH_ParamAccess.item);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "S", "True if the datagram was sent successfully", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Bytes Sent", "B", "Number of bytes sent", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string host = string.Empty;
        int port = 0;
        ByteArrayGoo? dataGoo = null;

        DA.GetData(0, ref host);
        DA.GetData(1, ref port);
        DA.GetData(2, ref dataGoo);

        var result = SendUtilities.SendUdpAsync(host, port, dataGoo?.Value ?? Array.Empty<byte>()).GetAwaiter().GetResult();
        if (result.Error is not null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, result.Error);
            if (result.IsValidationError) return;
        }
        if (result.Warning is not null) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, result.Warning);
        DA.SetData(0, result.Success);
        DA.SetData(1, result.BytesSent);

    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("A3F7B2C1-8D4E-4F5A-9B6C-1E2D3F4A5B6C");
}

