using Grasshopper.Kernel;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class ServerInputComponent : GH_Component, IGH_VariableParameterComponent
{
    private readonly ModernServerSession _session = new();
    private bool _requestTriggeredSolve;

    public ServerInputComponent()
        : base(
            "Server Input",
            "SI",
            "Listens for HTTP requests on a specified port and routes them to outputs based on the request path. Add outputs via the component's Zoomable UI (ZUI) to handle different routes.",
            ShellNaming.Category,
            ShellNaming.Server)
    {
        _session.RequestQueued += OnRequestQueued;
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Port", "P", "Port number to listen on (0-65535)", GH_ParamAccess.item, 8080);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        var rootParam = new ListenerRequestParam
        {
            Name = "Root",
            NickName = "/",
            Description = "Default route for requests to /",
            Access = GH_ParamAccess.item,
        };

        pManager.AddParameter(rootParam);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int port = 8080;
        DA.GetData(0, ref port);

        if (port < 0 || port > 65535)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port number must be between 0 and 65535");
            return;
        }

        string[] routes = Params.Output
            .Select(static output => ModernServerRouteMatcher.NormalizeRoute(output.NickName))
            .ToArray();

        try
        {
            _session.ReconfigureAsync(port, routes).GetAwaiter().GetResult();
            Message = $"http://localhost:{port}/";
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start server: {ex.Message}");
            return;
        }

        if (!_requestTriggeredSolve)
        {
            foreach (string route in routes)
            {
                while (_session.TryDequeuePendingRequest(route, out _))
                {
                }
            }
        }

        for (int i = 0; i < Params.Output.Count; i++)
        {
            if (!_requestTriggeredSolve)
            {
                DA.SetData(i, null);
                continue;
            }

            string route = ModernServerRouteMatcher.NormalizeRoute(Params.Output[i].NickName);
            if (_session.TryDequeuePendingRequest(route, out ModernServerRequestContext? context) &&
                context is not null)
            {
                DA.SetData(i, new ListenerRequestGoo(context));
            }
            else
            {
                DA.SetData(i, null);
            }
        }

        _requestTriggeredSolve = false;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Output;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Output && index > 0;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
        return new ListenerRequestParam
        {
            Name = $"Route {index}",
            NickName = "/new-route",
            Description = "HTTP route endpoint",
            Access = GH_ParamAccess.item,
        };
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => true;

    public void VariableParameterMaintenance()
    {
        foreach (IGH_Param output in Params.Output)
        {
            if (!output.NickName.StartsWith("/"))
            {
                output.NickName = "/" + output.NickName;
            }
        }
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        _session.RequestQueued -= OnRequestQueued;
        _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.DocumentContextChanged(document, context);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private void OnRequestQueued(object? sender, EventArgs e)
    {
        _requestTriggeredSolve = true;
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
    }
}

