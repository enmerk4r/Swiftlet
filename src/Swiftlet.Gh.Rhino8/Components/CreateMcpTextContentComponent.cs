using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Swiftlet.Core.Mcp;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class CreateMcpTextContentComponent : GH_Component
{
    public CreateMcpTextContentComponent()
        : base("Create MCP Text Content", "MCP TXT", "Creates a text content block for MCP Tool Response.", ShellNaming.Category, ShellNaming.Mcp)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Text", "T", "Text to show to the MCP client as part of the tool result.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new McpContentBlockParam(), "Content Block", "C", "Text content block for MCP Tool Response.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (HasJsonInput())
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You might want to use Stringify JSON first :)");
        }

        string text = string.Empty;
        if (!DA.GetData(0, ref text))
        {
            return;
        }

        DA.SetData(0, new McpContentBlockGoo(new McpTextContentBlock(text)));
    }

    private bool HasJsonInput()
    {
        IGH_Param inputParam = Params.Input[0];
        foreach (IGH_Param source in inputParam.Sources)
        {
            foreach (IGH_Goo goo in EnumerateData(source))
            {
                if (goo is JsonNodeGoo or JsonObjectGoo or JsonArrayGoo or JsonValueGoo)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<IGH_Goo> EnumerateData(IGH_Param param)
    {
        IGH_Structure structure = param.VolatileData;
        for (int pathIndex = 0; pathIndex < structure.PathCount; pathIndex++)
        {
            var branch = structure.get_Branch(pathIndex);
            foreach (object? item in branch)
            {
                if (item is IGH_Goo goo)
                {
                    yield return goo;
                }
            }
        }
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("77FF2424-16E0-4642-9E94-88933557B56F");
}
