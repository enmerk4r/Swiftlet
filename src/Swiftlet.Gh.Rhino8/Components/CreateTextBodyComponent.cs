using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class CreateTextBodyComponent : GH_Component
{
    private const string LegacyIsTextCheckedKey = "IsTextChecked";
    private const string LegacyIsJavascriptCheckedKey = "IsJavascriptChecked";
    private const string LegacyIsJsonCheckedKey = "IsJsonChecked";
    private const string LegacyIsHtmlCheckedKey = "IsHtmlChecked";
    private const string LegacyIsXmlCheckedKey = "IsXmlChecked";

    private string _contentType;
    private bool _isTextChecked;
    private bool _isJavascriptChecked;
    private bool _isJsonChecked;
    private bool _isHtmlChecked;
    private bool _isXmlChecked;

    public CreateTextBodyComponent()
        : base("Create Text Body", "CTB", "Create a Request Body that supports text formats", ShellNaming.Category, ShellNaming.Request)
    {
        _contentType = ContentTypes.ApplicationJson;
        _isJsonChecked = true;
    }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override bool Read(GH_IReader reader)
    {
        _isTextChecked = ReadBoolean(reader, LegacyIsTextCheckedKey, nameof(_isTextChecked));
        _isJavascriptChecked = ReadBoolean(reader, LegacyIsJavascriptCheckedKey, nameof(_isJavascriptChecked));
        _isJsonChecked = ReadBoolean(reader, LegacyIsJsonCheckedKey, nameof(_isJsonChecked));
        _isHtmlChecked = ReadBoolean(reader, LegacyIsHtmlCheckedKey, nameof(_isHtmlChecked));
        _isXmlChecked = ReadBoolean(reader, LegacyIsXmlCheckedKey, nameof(_isXmlChecked));

        if (_isTextChecked)
        {
            _contentType = ContentTypes.TextPlain;
        }
        else if (_isJavascriptChecked)
        {
            _contentType = ContentTypes.JavaScript;
        }
        else if (_isJsonChecked)
        {
            _contentType = ContentTypes.ApplicationJson;
        }
        else if (_isHtmlChecked)
        {
            _contentType = ContentTypes.TextHtml;
        }
        else if (_isXmlChecked)
        {
            _contentType = ContentTypes.ApplicationXml;
        }

        Message = ContentTypes.ToDisplayName(_contentType);
        return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.SetBoolean(LegacyIsTextCheckedKey, _isTextChecked);
        writer.SetBoolean(LegacyIsJavascriptCheckedKey, _isJavascriptChecked);
        writer.SetBoolean(LegacyIsJsonCheckedKey, _isJsonChecked);
        writer.SetBoolean(LegacyIsHtmlCheckedKey, _isHtmlChecked);
        writer.SetBoolean(LegacyIsXmlCheckedKey, _isXmlChecked);
        return base.Write(writer);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Content", "C", "Text contents of your request body", GH_ParamAccess.item);
        pManager[0].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new RequestBodyParam(), "Body", "B", "Request Body", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object input = null;
        string text = string.Empty;
        string detectedContentType = null;

        DA.GetData(0, ref input);

        if (input is null)
        {
        }
        else if (input is GH_String grasshopperString)
        {
            text = grasshopperString.ToString();
        }
        else if (input is JsonArrayGoo jsonArrayGoo)
        {
            text = jsonArrayGoo.Value?.ToJsonString() ?? string.Empty;
            detectedContentType = ContentTypes.ApplicationJson;
        }
        else if (input is JsonObjectGoo jsonObjectGoo)
        {
            text = jsonObjectGoo.Value?.ToJsonString() ?? string.Empty;
            detectedContentType = ContentTypes.ApplicationJson;
        }
        else if (input is JsonNodeGoo jsonNodeGoo)
        {
            text = jsonNodeGoo.Value?.ToJsonString() ?? "null";
            detectedContentType = ContentTypes.ApplicationJson;
        }
        else if (input is JsonValueGoo jsonValueGoo)
        {
            text = jsonValueGoo.Value?.ToJsonString() ?? "null";
            detectedContentType = ContentTypes.ApplicationJson;
        }
        else if (input is XmlNodeGoo xmlGoo && xmlGoo.Value is not null)
        {
            text = xmlGoo.Value.OuterXml;
            detectedContentType = ContentTypes.ApplicationXml;
        }
        else if (input is HtmlNodeGoo htmlGoo && htmlGoo.Value is not null)
        {
            text = htmlGoo.Value.OuterHtml;
            detectedContentType = ContentTypes.TextHtml;
        }
        else
        {
            throw new Exception("Content must be a string, JObject, JArray, XML Node, or HTML Node");
        }

        if (detectedContentType is not null)
        {
            SetContentType(detectedContentType);
        }

        RequestBodyText body = new(_contentType, text);
        DA.SetData(0, new RequestBodyGoo(body));
        Message = ContentTypes.ToDisplayName(_contentType);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D5BF7D4A-9FC3-4984-90F7-FEB32AA96D9F");

    private void SetContentType(string contentType)
    {
        _contentType = contentType;
        UncheckAll();

        switch (contentType)
        {
            case ContentTypes.TextPlain:
                _isTextChecked = true;
                break;
            case ContentTypes.JavaScript:
                _isJavascriptChecked = true;
                break;
            case ContentTypes.ApplicationJson:
                _isJsonChecked = true;
                break;
            case ContentTypes.TextHtml:
                _isHtmlChecked = true;
                break;
            case ContentTypes.ApplicationXml:
                _isXmlChecked = true;
                break;
        }
    }

    private void UncheckAll()
    {
        _isTextChecked = false;
        _isJavascriptChecked = false;
        _isJsonChecked = false;
        _isHtmlChecked = false;
        _isXmlChecked = false;
    }

    private static bool ReadBoolean(GH_IReader reader, string preferredKey, string fallbackKey)
    {
        if (reader.ItemExists(preferredKey))
        {
            return reader.GetBoolean(preferredKey);
        }

        if (reader.ItemExists(fallbackKey))
        {
            return reader.GetBoolean(fallbackKey);
        }

        return false;
    }
}
