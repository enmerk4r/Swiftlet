using Eto.Forms;
using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI.InputPanel;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components.Body;

// "2. Request" tab: request bodies and multipart fields. Content conversion is shared through
// Swiftlet.Runtime.RequestBodyContent; GH1 does the same for its goo in BodyInputConverter.

[IoId(ComponentIds.CreateTextBody)]
public sealed class CreateTextBodyComponent : SwiftletComponent
{
    internal const string ContentTypeKey = "ContentType";

    /// <summary>The GH1 context-menu choices, in menu order.</summary>
    internal static readonly (string Label, string ContentType)[] Choices =
    {
        ("Text", ContentTypes.TextPlain),
        ("JavaScript", ContentTypes.JavaScript),
        ("JSON", ContentTypes.ApplicationJson),
        ("HTML", ContentTypes.TextHtml),
        ("XML", ContentTypes.ApplicationXml),
    };

    public CreateTextBodyComponent()
        : base("Create Text Body", "Create a Request Body that supports text formats", Sections.Request) { }

    public CreateTextBodyComponent(IReader reader) : base(reader)
    {
        if (reader.HasItem(ContentTypeKey))
            ContentType = reader.String(ContentTypeKey);
    }

    /// <summary>Content type used for plain-text content (the GH1 menu setting; JSON by default).</summary>
    public string ContentType
    {
        get => CustomValues.Get(ContentTypeKey, ContentTypes.ApplicationJson);
        set => CustomValues.Set(ContentTypeKey, string.IsNullOrWhiteSpace(value) ? ContentTypes.ApplicationJson : value);
    }

    public override void Store(IWriter writer)
    {
        base.Store(writer);
        writer.String(ContentTypeKey, ContentType);
    }

    protected override void AddInputs(InputAdder inputs) =>
        inputs.AddGeneric("Content", "C", "Text contents of your request body", Access.Item, Requirement.MayBeMissing);

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new RequestBodyParameter("Body", "B", "Request Body"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out object content);
        // Unsupported content throws, as in GH1 (empty output rather than a null item).
        string text = RequestBodyContent.ToText(content);

        // JSON/XML/HTML content selects its own content type, as in GH1. Unlike GH1 this does not
        // change the stored setting: GH2 solves run in parallel and must not mutate the component.
        string contentType = RequestBodyContent.DetectContentType(content) ?? ContentType;
        access.SetItem(0, new RequestBodyText(contentType, text));
        ShowMessage(ContentTypes.ToDisplayName(contentType));
    }

    public override void AppendToInputPanel(InputPanel panel)
    {
        base.AppendToInputPanel(panel);
        panel.AddLabel("Content type", false, "Content-Type used for plain text content");
        var picker = new DropDown();
        foreach (var (label, _) in Choices)
            picker.Items.Add(label);
        picker.SelectedIndex = Array.FindIndex(Choices, c => string.Equals(c.ContentType, ContentType, StringComparison.OrdinalIgnoreCase));
        picker.SelectedIndexChanged += (_, _) =>
        {
            if (picker.SelectedIndex < 0)
                return;
            AddUndoRecord(new Grasshopper2.Undo.VerbNoun("Change", "Content type"),
                new Grasshopper2.Undo.Actions.CustomValueAction(this, ContentTypeKey));
            ContentType = Choices[picker.SelectedIndex].ContentType;
            Expire();
            Document?.Solution.Start();
        };
        panel.Add(picker);
    }
}

[IoId(ComponentIds.CreateTextBodyCustom)]
public sealed class CreateTextBodyCustomComponent : SwiftletComponent
{
    public CreateTextBodyCustomComponent()
        : base("Create Text Body Custom", "Create a Request Body that supports text formats with a custom Content-Type header", Sections.Request) { }

    public CreateTextBodyCustomComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddGeneric("Content", "C", "Text contents of your request body", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("ContentType", "T", "Content-Type header value", Access.Item, Requirement.MayBeMissing).Set(ContentTypes.ApplicationJson);
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new RequestBodyParameter("Body", "B", "Request Body"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out object content);
        if (!access.GetItem(1, out string contentType))
            contentType = ContentTypes.ApplicationJson;
        // Unsupported content throws, as in GH1.
        access.SetItem(0, new RequestBodyText(contentType, RequestBodyContent.ToText(content)));
    }
}

[IoId(ComponentIds.CreateByteArrayBody)]
public sealed class CreateByteArrayBodyComponent : SwiftletComponent
{
    public CreateByteArrayBodyComponent()
        : base("Create Byte Array Body", "Create a Request Body that supports Byte Array content", Sections.Request) { }

    public CreateByteArrayBodyComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddGeneric("Content", "C", "Input content (byte array, string, JSON, XML, or HTML)");
        inputs.AddText("ContentType", "T", "Content-Type header value");
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new RequestBodyParameter("Body", "B", "Request Body"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out object content);
        access.GetItem(1, out string contentType);
        // Unsupported content throws, as in GH1.
        access.SetItem(0, new RequestBodyBytes(contentType, RequestBodyContent.ToBytes(content)));
    }
}

[IoId(ComponentIds.CreateByteArrayBodyFromFile)]
public sealed class CreateByteArrayBodyFromFileComponent : SwiftletComponent
{
    public CreateByteArrayBodyFromFileComponent()
        : base("Create Byte Array Body from File", "Create a Request Body that supports Byte Array content by providing a filepath", Sections.Request) { }

    public CreateByteArrayBodyFromFileComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Path", "P", "Path to file");
        inputs.AddText("ContentType", "T", "Content-Type header value");
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new RequestBodyParameter("Body", "B", "Request Body"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string path);
        access.GetItem(1, out string contentType);
        try
        {
            byte[] content = File.ReadAllBytes(FilePathUtility.NormalizePath(path));
            access.SetItem(0, new RequestBodyBytes(contentType, content));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            access.AddError("Failed to read file", $"Failed to read file: {ex.Message}");
        }
    }
}

[IoId(ComponentIds.CreateFormUrlEncodedBody)]
public sealed class CreateFormUrlEncodedBodyComponent : SwiftletComponent
{
    public CreateFormUrlEncodedBodyComponent()
        : base("Create Form URL Encoded Body",
            "Create a request body with application/x-www-form-urlencoded content type. Commonly used for HTML form submissions and OAuth token endpoints.",
            Sections.Request) { }

    public CreateFormUrlEncodedBodyComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Keys", "K", "Form field names", Access.Twig);
        inputs.AddText("Values", "V", "Form field values", Access.Twig);
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new RequestBodyParameter("Body", "B", "Form URL encoded request body"));

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<string>(0, out var keyTwig);
        access.GetTwig<string>(1, out var valueTwig);
        var keys = keyTwig?.NonNullItems.ToList() ?? new List<string>();
        var values = valueTwig?.NonNullItems.ToList() ?? new List<string>();
        if (keys.Count != values.Count)
        {
            access.AddError("Key/value count mismatch", $"Keys and values must have the same count. Got {keys.Count} keys and {values.Count} values.");
            return;
        }

        if (keys.Count == 0)
            access.AddWarning("No form fields provided", "Both lists are empty.");
        access.SetItem(0, new RequestBodyFormUrlEncoded(keys, values));
        ShowMessage("x-www-form-urlencoded");
    }
}

[IoId(ComponentIds.CreateMultipartFieldText)]
public sealed class CreateMultipartFieldTextComponent : SwiftletComponent
{
    public CreateMultipartFieldTextComponent()
        : base("Create Multipart Field Text", "Create a text multipart/form-data field", Sections.Request) { }

    public CreateMultipartFieldTextComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Name", "N", "Field name (optional)", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("Text", "T", "Field text value", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("ContentType", "C", "Field content type", Access.Item, Requirement.MayBeMissing);
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new MultipartFieldParameter("Field", "F", "Multipart field"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out string name)) name = string.Empty;
        if (!access.GetItem(1, out string text)) text = string.Empty;
        if (!access.GetItem(2, out string contentType) || string.IsNullOrWhiteSpace(contentType))
            contentType = ContentTypes.TextPlain;
        access.SetItem(0, new MultipartField(name, text, contentType));
    }
}

[IoId(ComponentIds.CreateMultipartFieldBytes)]
public sealed class CreateMultipartFieldBytesComponent : SwiftletComponent
{
    public CreateMultipartFieldBytesComponent()
        : base("Create Multipart Field Bytes", "Create a byte multipart/form-data field", Sections.Request) { }

    public CreateMultipartFieldBytesComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Name", "N", "Field name (optional)", Access.Item, Requirement.MayBeMissing);
        // InputAdder.Add resets Requirement to its own argument (default MustExist): pass it explicitly.
        inputs.Add(new ByteArrayParameter("Bytes", "B", "Field bytes"), Requirement.MayBeMissing);
        inputs.AddText("FileName", "F", "Optional filename", Access.Item, Requirement.MayBeMissing);
        inputs.AddText("ContentType", "C", "Field content type", Access.Item, Requirement.MayBeMissing);
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new MultipartFieldParameter("Field", "F", "Multipart field"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out string name)) name = string.Empty;
        if (!access.GetItem(1, out byte[] bytes)) bytes = Array.Empty<byte>();
        if (!access.GetItem(2, out string fileName)) fileName = string.Empty;
        if (!access.GetItem(3, out string contentType) || string.IsNullOrWhiteSpace(contentType))
            contentType = ContentTypes.ApplicationOctetStream;
        access.SetItem(0, new MultipartField(name, bytes, fileName, contentType));
    }
}

[IoId(ComponentIds.CreateMultipartFormBody)]
public sealed class CreateMultipartFormBodyComponent : SwiftletComponent
{
    public CreateMultipartFormBodyComponent()
        : base("Create Multipart Form Body", "Create a Request Body that supports multipart/form-data", Sections.Request) { }

    public CreateMultipartFormBodyComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        var fields = new MultipartFieldParameter("Fields", "F", "Multipart form fields", Access.Twig);
        fields.Set(Array.Empty<MultipartField>());
        inputs.Add(fields, Requirement.MayBeMissing); // Add resets Requirement unless passed explicitly
    }

    protected override void AddOutputs(OutputAdder outputs) =>
        outputs.Add(new RequestBodyParameter("Body", "B", "Request Body"));

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<MultipartField>(0, out var fields);
        access.SetItem(0, new RequestBodyMultipartForm(fields?.NonNullItems.ToArray() ?? Array.Empty<MultipartField>()));
    }
}
