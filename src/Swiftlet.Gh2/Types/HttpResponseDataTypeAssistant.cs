using System.Text.Json;
using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Core.Http;
namespace Swiftlet.Gh2.Types;

public sealed class HttpResponseDataTypeAssistant : TypeAssistant<HttpResponseData>
{
    public HttpResponseDataTypeAssistant() : base("HTTP Response") { }
    public override HttpResponseData Copy(HttpResponseData instance) => instance.Duplicate();
    public override string DescribePrimary(Pear<HttpResponseData> pear) => $"{pear.Item.StatusCode} {pear.Item.ReasonPhrase}";
    public override bool Write(IWriter writer, Name location, HttpResponseData instance)
    {
        writer.String(location, JsonSerializer.Serialize(instance));
        return true;
    }
    public override bool Read(IReader reader, Name location, out HttpResponseData instance)
    {
        instance = null!;
        if (!reader.HasItem(location)) return false;
        using var document = JsonDocument.Parse(reader.String(location));
        var root = document.RootElement;
        instance = new HttpResponseData(root.GetProperty("Version").GetString()!,
            root.GetProperty("StatusCode").GetInt32(), root.GetProperty("ReasonPhrase").GetString(),
            root.GetProperty("Headers").EnumerateArray().Select(h => new HttpHeader(h.GetProperty("Key").GetString()!, h.GetProperty("Value").GetString()!)),
            root.GetProperty("IsSuccessStatusCode").GetBoolean(), root.GetProperty("Content").GetString()!,
            root.GetProperty("Bytes").GetBytesFromBase64());
        return true;
    }
}
