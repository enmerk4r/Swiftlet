using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh2.Types;

public sealed class HttpHeaderTypeAssistant : TypeAssistant<HttpHeader>
{
    public HttpHeaderTypeAssistant() : base("HTTP Header") { }

    public override HttpHeader Copy(HttpHeader instance) => new(instance.Key, instance.Value);

    public override string DescribePrimary(Pear<HttpHeader> pear)
        => $"{pear.Item.Key}: {pear.Item.Value}";

    public override bool Write(IWriter writer, Name location, HttpHeader instance)
    {
        writer.StringArray(location, new[] { instance.Key, instance.Value });
        return true;
    }

    public override bool Read(IReader reader, Name location, out HttpHeader instance)
    {
        instance = null!;
        if (!reader.HasItem(location))
            return false;
        var parts = reader.StringArray(location);
        if (parts.Length != 2 || parts[0] is null || parts[1] is null)
            return false;
        instance = new HttpHeader(parts[0], parts[1]);
        return true;
    }
}
