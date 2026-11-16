using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.Icon;
using GrasshopperIO;
using Swiftlet.Gh2.Icons;

namespace Swiftlet.Gh2.Components;

[IoId(ComponentIds.CreateUrl)]
public sealed class CreateUrlComponent : Component
{
    public CreateUrlComponent()
        : base(Palette.Entry(nameof(CreateUrlComponent), "Create URL", "Construct a URL from its constituent parts", "Swiftlet", "2. Request")) { }

    public CreateUrlComponent(IReader reader) : base(reader) { }

    protected override IIcon IconInternal => SwiftletIcons.For(GetType()) ?? base.IconInternal;

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Scheme", "S", "URL scheme (http or https)");
        inputs.AddText("Host", "H", "Host component of the URL");
        inputs.AddInteger("Port", "P", "TCP port number").Requirement = Requirement.MayBeMissing;
        inputs.AddText("Route", "R", "A route (path) to an online resource").Requirement = Requirement.MayBeMissing;
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddText("URL", "U", "Constructed URL string");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string scheme);
        access.GetItem(1, out string host);
        if (!access.GetItem(2, out int port))
            port = -1;
        if (!access.GetItem(3, out string route))
            route = string.Empty;

        // Same construction as GH1 (UriBuilder; non-positive port means "scheme default").
        UriBuilder builder;
        try
        {
            builder = new UriBuilder { Scheme = scheme, Host = host };
            if (port > 0)
                builder.Port = port;
            if (!string.IsNullOrEmpty(route))
                builder.Path = route;
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException)
        {
            access.AddError("Invalid URL part", ex.Message);
            return;
        }

        access.SetItem(0, builder.ToString());
    }
}
