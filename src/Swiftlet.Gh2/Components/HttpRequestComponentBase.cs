using Eto.Forms;
using Grasshopper2.Components;
using Grasshopper2.Parameters;
using Grasshopper2.UI;
using Grasshopper2.UI.InputPanel;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Core.Security;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

public abstract class HttpRequestComponentBase : Component
{
    protected override Grasshopper2.UI.Icon.IIcon IconInternal => Icons.SwiftletIcons.For(GetType()) ?? base.IconInternal;

    private static readonly int[] TimeoutOptions = { 1, 5, 10, 15, 30, 60, 100, 300, 600, 900 };
    // HttpClient permits parallel SendAsync calls and is retained for connection reuse.
    private static readonly HttpRequestExecutor Executor = new();
    private readonly IpBlacklist _ipBlacklist = IpBlacklist.FromEnvironment();

    protected HttpRequestComponentBase(string name, string description) : base(Palette.Entry(nameof(HttpRequestComponentBase), name, description, "Swiftlet", "3. Send")) { }
    protected virtual string? FixedMethod => null;
    protected virtual bool SupportsBody => false;
    protected virtual string BodyDescription => string.Empty;
    protected HttpRequestComponentBase(IReader reader) : base(reader)
    {
        if (reader.HasItem("TimeoutSeconds"))
            TimeoutSeconds = reader.Integer32("TimeoutSeconds");
    }

    public int TimeoutSeconds
    {
        get => CustomValues.Get("TimeoutSeconds", 100);
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            CustomValues.Set("TimeoutSeconds", value);
        }
    }

    public override void Store(IWriter writer)
    {
        base.Store(writer);
        writer.Integer32("TimeoutSeconds", TimeoutSeconds);
    }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URL", "U", "URL for the web resource you're trying to reach");
        if (FixedMethod is null) inputs.AddText("Method", "M", "HTTP method: \"GET\", \"POST\", \"PUT\", \"DELETE\", \"PATCH\", \"HEAD\", \"CONNECT\", \"OPTIONS\", \"TRACE\"");
        if (SupportsBody)
        {
            var body = new RequestBodyParameter("Body", "B", BodyDescription);
            inputs.Add(body, FixedMethod is null ? Requirement.MayBeMissing : Requirement.MustExist);
        }
        var query = new QueryParameterParameter("Params", "P", "Query Params", Access.Twig);
        var headers = new HttpHeaderParameter("Headers", "H", "Http Headers", Access.Twig);
        inputs.Add(query, Requirement.MayBeMissing);
        inputs.Add(headers, Requirement.MayBeMissing);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddInteger("Status", "S", "Http Status Code");
        outputs.AddText("Content", "C", "Http response body");
        outputs.Add(new HttpResponseDataParameter("Response", "R", "Full Http response object (with metadata)"));
    }

    protected override void Process(IDataAccess access)
    {
        var token = access.Solution.Token;
        token.ThrowIfCancellationRequested();
        access.GetItem(0, out string url);
        var validation = UrlValidator.ValidateHttpUrl(url, _ipBlacklist);
        if (!validation.IsValid)
        {
            access.AddError("Invalid URL", validation.ErrorMessage);
            return;
        }
        token.ThrowIfCancellationRequested();
        int index = 1;
        string method = FixedMethod ?? string.Empty;
        if (FixedMethod is null) access.GetItem(index++, out method);
        IRequestBody? body = null;
        if (SupportsBody) access.GetItem(index++, out body);
        access.GetTwig<QueryParameter>(index++, out var query);
        access.GetTwig<HttpHeader>(index, out var headers);
        var request = ModernRequestWorkflow.CreateRequest(url, method, body,
            query?.NonNullItems, headers?.NonNullItems, TimeoutSeconds);
        // GH2 Process is synchronous and already runs off the UI thread. Its cancellation
        // token flows through async network I/O; never create an untracked Task.Run here.
        var response = Executor.ExecuteAsync(request, token).GetAwaiter().GetResult();
        token.ThrowIfCancellationRequested();
        access.SetItem(0, response.StatusCode);
        access.SetItem(1, response.Content);
        access.SetItem(2, response);
    }

    public override void AppendToInputPanel(InputPanel panel)
    {
        base.AppendToInputPanel(panel);
        panel.AddLabel("Timeout", false, "HTTP timeout in seconds");
        var picker = new DropDown();
        foreach (int seconds in TimeoutOptions) picker.Items.Add($"{seconds} seconds");
        picker.SelectedIndex = Array.IndexOf(TimeoutOptions, TimeoutSeconds);
        picker.SelectedIndexChanged += (_, _) =>
        {
            if (picker.SelectedIndex < 0) return;
            AddUndoRecord(new Grasshopper2.Undo.VerbNoun("Change", "Timeout"),
                new Grasshopper2.Undo.Actions.CustomValueAction(this, "TimeoutSeconds"));
            TimeoutSeconds = TimeoutOptions[picker.SelectedIndex];
            Expire();
            Document?.Solution.Start();
        };
        panel.Add(picker);
    }
}
