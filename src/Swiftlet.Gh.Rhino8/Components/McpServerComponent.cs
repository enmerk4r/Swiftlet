using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class McpServerComponent : GH_Component, IGH_VariableParameterComponent
{
    private readonly ModernMcpServerSession _session = new();
    private readonly RhinoHostServices _hostServices = new();
    private IReadOnlyList<McpToolDefinition> _tools = [];
    private bool _requestTriggeredSolve;

    public McpServerComponent()
        : base("MCP Server", "MCP", "An MCP server that exposes Grasshopper tools to AI clients like Claude.", ShellNaming.Category, ShellNaming.Mcp)
    {
        _session.RequestQueued += OnRequestQueued;
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Port", "P", "Port number to listen on (default: 3001)", GH_ParamAccess.item, 3001);
        pManager.AddParameter(new McpToolDefinitionParam(), "Tools", "T", "Tool definitions to expose", GH_ParamAccess.list);
        pManager.AddTextParameter("Server Name", "N", "Server name for MCP protocol", GH_ParamAccess.item, "Swiftlet");
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int port = 3001;
        List<McpToolDefinitionGoo> toolGoos = [];
        string serverName = "Swiftlet";

        DA.GetData(0, ref port);
        DA.GetDataList(1, toolGoos);
        DA.GetData(2, ref serverName);

        if (port < 0 || port > 65535)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Port number must be between 0 and 65535");
            return;
        }

        _tools = toolGoos
            .Where(static goo => goo?.Value is not null)
            .Select(static goo => goo!.Value!)
            .ToList();

        if (NeedsOutputUpdate())
        {
            ScheduleOutputUpdate();
            return;
        }

        try
        {
            _session.ReconfigureAsync(port, serverName, _tools).GetAwaiter().GetResult();
            Message = _session.StatusMessage ?? string.Empty;
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to start MCP server: {ex.Message}");
            Message = "Error";
            return;
        }

        if (!_requestTriggeredSolve)
        {
            foreach (McpToolDefinition tool in _tools)
            {
                while (_session.TryDequeuePendingCall(tool.Name, out _))
                {
                }
            }
        }

        for (int i = 0; i < Params.Output.Count; i++)
        {
            string outputName = Params.Output[i].NickName;
            string toolName = outputName;
            if (_requestTriggeredSolve &&
                _session.TryDequeuePendingCall(toolName, out ModernMcpToolCallContext? context) &&
                context is not null)
            {
                DA.SetData(i, new McpToolCallRequestGoo(context));
            }
            else
            {
                DA.SetData(i, null);
            }
        }

        _requestTriggeredSolve = false;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
        return new Param_GenericObject();
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => true;

    public void VariableParameterMaintenance()
    {
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

    public override Guid ComponentGuid => new("D4E5F6A7-B8C9-0123-DEF0-234567890123");

    private bool NeedsOutputUpdate()
    {
        HashSet<string> currentOutputs = Params.Output.Select(static output => output.NickName).ToHashSet(StringComparer.Ordinal);
        HashSet<string> desiredOutputs = GetDesiredOutputNames();
        return !currentOutputs.SetEquals(desiredOutputs);
    }

    private void ScheduleOutputUpdate()
    {
        GH_Document? document = OnPingDocument();
        document?.ScheduleSolution(5, _ => UpdateOutputsFromTools());
    }

    private void UpdateOutputsFromTools()
    {
        HashSet<string> desiredOutputs = GetDesiredOutputNames();
        bool changed = false;

        for (int i = Params.Output.Count - 1; i >= 0; i--)
        {
            if (!desiredOutputs.Contains(Params.Output[i].NickName))
            {
                Params.UnregisterOutputParameter(Params.Output[i]);
                changed = true;
            }
        }

        HashSet<string> currentOutputs = Params.Output.Select(static output => output.NickName).ToHashSet(StringComparer.Ordinal);
        foreach (McpToolDefinition tool in _tools)
        {
            if (currentOutputs.Contains(tool.Name))
            {
                continue;
            }

            var param = new McpToolCallRequestParam
            {
                Name = tool.Name,
                NickName = tool.Name,
                Description = $"Tool call request for '{tool.Name}'",
                Access = GH_ParamAccess.item,
            };

            Params.RegisterOutputParam(param);
            changed = true;
        }

        if (changed)
        {
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
    }

    private void OnRequestQueued(object? sender, EventArgs e)
    {
        _requestTriggeredSolve = true;
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
    }

    private bool TryBuildConfig(out string? config, out string? error)
    {
        try
        {
            config = _session.GenerateConfig(GetType().Assembly.Location);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            config = null;
            error = $"Failed to generate MCP config: {ex.Message}";
            return false;
        }
    }

    private bool CopyConfigToClipboard()
    {
        if (!TryBuildConfig(out string? config, out string? configError) || string.IsNullOrWhiteSpace(config))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, configError ?? "Failed to generate MCP config.");
            return false;
        }

        HostActionResult result = _session.ExportConfigAsync(_hostServices, GetType().Assembly.Location).GetAwaiter().GetResult();

        GH_RuntimeMessageLevel level = result.IsSuccess
            ? GH_RuntimeMessageLevel.Remark
            : GH_RuntimeMessageLevel.Warning;

        string message = result.RequiresManualAction
            ? result.ManualActionText ?? result.Message
            : result.Message;

        AddRuntimeMessage(level, message);
        return result.IsSuccess;
    }

    private HashSet<string> GetDesiredOutputNames()
    {
        return _tools.Select(static tool => tool.Name).ToHashSet(StringComparer.Ordinal);
    }
}

