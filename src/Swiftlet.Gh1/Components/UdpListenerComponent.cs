
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UdpListenerComponent : GH_Component
{
    private readonly UdpListenerSession _session = new();
    private bool _freeze;

    public UdpListenerComponent()
        : base("UDP Listener", "UDP-L", "A UDP listener component that receives UDP datagrams on a specified port.\nThis component is ALPHA. Use at your own risk.", ShellNaming.Category, ShellNaming.Server)
    {
        _session.DataReceived += OnDataReceived;
        _session.Faulted += OnFaulted;
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Port", "P", "Port number to listen on (0-65535)", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ByteArrayParam(), "Data", "D", "Received data as byte array", GH_ParamAccess.item);
        pManager.AddTextParameter("Remote", "R", "Remote endpoint (IP:Port) that sent the data", GH_ParamAccess.item);
    }

    protected override void BeforeSolveInstance()
    {
        _freeze = true;
        base.BeforeSolveInstance();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int port = 0;
        if (!DA.GetData(0, ref port))
        {
            return;
        }

        if (port < 0 || port > 65535)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port must be between 0 and 65535");
            return;
        }

        Message = $":{port}";

        try
        {
            _session.Start(port);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start listener: {ex.Message}");
        }

        if (_session.LatestData is { } data)
        {
            DA.SetData(0, new ByteArrayGoo(data));
        }

        DA.SetData(1, _session.LatestRemoteEndpoint);
    }

    protected override void AfterSolveInstance()
    {
        _freeze = false;
        base.AfterSolveInstance();
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        _session.Stop();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            _session.Stop();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B4E8C3D2-9F5A-4E6B-8C7D-2F3E4A5B6C7D");

    private void OnDataReceived(object? sender, EventArgs e)
    {
        if (!_freeze)
        {
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
        }
    }

    private void OnFaulted(object? sender, string message)
    {
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() => AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message)));
    }
}
