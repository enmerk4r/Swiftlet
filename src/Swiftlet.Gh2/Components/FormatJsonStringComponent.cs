using System.Text.Json;
using System.Text.Json.Nodes;
using Grasshopper2.Components;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.FormatJsonString)]
public sealed class FormatJsonStringComponent : Component
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public FormatJsonStringComponent()
        : base(Palette.Entry(nameof(FormatJsonStringComponent), "Format Json String", "Prettify a JSON string by formatting it with proper indentations", "Swiftlet", "4. Read JSON")) { }

    public FormatJsonStringComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("JSON String", "Js", "Unformatted JSON string");
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("Pretty JSON", "Pj", "Formatted JSON string (this helps with readability)");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string json);
        try
        {
            JsonNode? node = JsonNode.Parse(json);
            if (node is null)
            {
                access.AddError("Unable to parse JSON string", "The input parsed to a JSON null.");
                return;
            }

            access.SetItem(0, node.ToJsonString(Indented));
        }
        catch (JsonException ex)
        {
            access.AddError("Invalid JSON", ex.Message);
        }
    }
}
