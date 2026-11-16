using System.Net.Sockets;
using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UdpListenerComponent : GH_Component
{
    private UdpClient? _client;
    private Task? _listenerTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _port;
    private byte[]? _lastData;
    private string? _lastRemoteEndpoint;
    private bool _freeze;

    public UdpListenerComponent()
        : base("UDP Listener", "UDP-L", "A UDP listener component that receives UDP datagrams on a specified port.\nThis component is ALPHA. Use at your own risk.", ShellNaming.Category, ShellNaming.Server)
    {
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

        bool portChanged = _port != port;
        _port = port;
        Message = $":{port}";

        if (_client is null || portChanged)
        {
            StopListener();
            StartListener(port);
        }

        if (_lastData is not null)
        {
            DA.SetData(0, new ByteArrayGoo(_lastData));
        }

        DA.SetData(1, _lastRemoteEndpoint);
    }

    protected override void AfterSolveInstance()
    {
        _freeze = false;
        base.AfterSolveInstance();
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        StopListener();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            StopListener();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("B4E8C3D2-9F5A-4E6B-8C7D-2F3E4A5B6C7D");

    private void StartListener(int port)
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _client = new UdpClient(port);
            _listenerTask = Task.Run(() => ListenForDataAsync(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start listener: {ex.Message}");
        }
    }

    private void StopListener()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _client?.Close();
            _client?.Dispose();
            _client = null;
            _listenerTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        catch
        {
        }
    }

    private async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _client is not null)
            {
                UdpReceiveResult result = await _client.ReceiveAsync().ConfigureAwait(false);
                _lastData = result.Buffer;
                _lastRemoteEndpoint = $"{result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port}";

                if (!_freeze)
                {
                    Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
        catch (Exception ex)
        {
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() => AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message)));
        }
    }
}
