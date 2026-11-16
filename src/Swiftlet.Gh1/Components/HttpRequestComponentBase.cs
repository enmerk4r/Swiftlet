using System.Net;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Core.Security;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public abstract partial class HttpRequestComponentBase : GH_TaskCapableComponent<HttpRequestSolveResults>
{
    protected static readonly int[] TimeoutOptions = { 1, 5, 10, 15, 30, 60, 100, 300, 600, 900 };

    private readonly HttpRequestExecutor _executor = new();
    private readonly IpBlacklist _ipBlacklist = IpBlacklist.FromEnvironment();
    protected int TimeoutSeconds { get; set; } = 100;

    protected virtual string? FixedMethod => null;

    protected virtual bool SupportsBody => false;

    protected virtual string BodyDescription => string.Empty;

    protected HttpRequestComponentBase(
        string name,
        string nickname,
        string description)
        : base(name, nickname, description, ShellNaming.Category, ShellNaming.Send)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "URL for the web resource you're trying to reach", GH_ParamAccess.item);

        int nextIndex = 1;
        if (FixedMethod is null)
        {
            pManager.AddTextParameter("Method", "M", "HTTP method: \"GET\", \"POST\", \"PUT\", \"DELETE\", \"PATCH\", \"HEAD\", \"CONNECT\", \"OPTIONS\", \"TRACE\"", GH_ParamAccess.item);
            nextIndex++;
        }

        if (SupportsBody)
        {
            pManager.AddParameter(new RequestBodyParam(), "Body", "B", BodyDescription, GH_ParamAccess.item);
            pManager[nextIndex].Optional = FixedMethod is null;
            nextIndex++;
        }

        pManager.AddParameter(new QueryParameterParam(), "Params", "P", "Query Params", GH_ParamAccess.list);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "Http Headers", GH_ParamAccess.list);
        pManager[nextIndex].Optional = true;
        pManager[nextIndex + 1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("Status", "S", "Http Status Code", GH_ParamAccess.item);
        pManager.AddTextParameter("Content", "C", "Http response body", GH_ParamAccess.item);
        pManager.AddParameter(new HttpResponseDataParam(), "Response", "R", "Full Http response object (with metadata)", GH_ParamAccess.item);
    }

    public override bool Read(GH_IReader reader)
    {
        if (reader.ItemExists("TimeoutSeconds"))
        {
            TimeoutSeconds = reader.GetInt32("TimeoutSeconds");
        }

        return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
        writer.SetInt32("TimeoutSeconds", TimeoutSeconds);
        return base.Write(writer);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        if (InPreSolve)
        {
            RequestInputs inputs = ReadInputs(DA);
            ValidateUrl(inputs.Url);

            int timeout = TimeoutSeconds;
            TaskList.Add(Task.Run(
                () => new HttpRequestSolveResults
                {
                    Value = Execute(inputs.Url, inputs.Method, inputs.Body, inputs.QueryParameters, inputs.Headers, timeout),
                },
                CancelToken));

            return;
        }

        if (!GetSolveResults(DA, out HttpRequestSolveResults result))
        {
            RequestInputs inputs = ReadInputs(DA);
            ValidateUrl(inputs.Url);
            result = new HttpRequestSolveResults
            {
                Value = Execute(inputs.Url, inputs.Method, inputs.Body, inputs.QueryParameters, inputs.Headers, TimeoutSeconds),
            };
        }

        if (result?.Value is null)
        {
            return;
        }

        DA.SetData(0, result.Value.StatusCode);
        DA.SetData(1, result.Value.Content);
        DA.SetData(2, new HttpResponseDataGoo(result.Value));
    }

    private RequestInputs ReadInputs(IGH_DataAccess DA)
    {
        string url = string.Empty;
        string method = FixedMethod ?? string.Empty;
        RequestBodyGoo? bodyGoo = null;
        List<QueryParameterGoo> queryParameters = [];
        List<HttpHeaderGoo> headers = [];

        int inputIndex = 0;
        DA.GetData(inputIndex++, ref url);

        if (FixedMethod is null)
        {
            DA.GetData(inputIndex++, ref method);
        }

        if (SupportsBody)
        {
            DA.GetData(inputIndex++, ref bodyGoo);
        }

        DA.GetDataList(inputIndex++, queryParameters);
        DA.GetDataList(inputIndex, headers);

        return new RequestInputs(url, method, bodyGoo?.Value, queryParameters, headers);
    }

    private HttpResponseData Execute(
        string url,
        string method,
        IRequestBody? body,
        IEnumerable<QueryParameterGoo> queryParameters,
        IEnumerable<HttpHeaderGoo> headers,
        int timeoutSeconds)
    {
        HttpRequestDefinition request = ModernRequestWorkflow.CreateRequest(
            url,
            method,
            body,
            queryParameters.Where(static goo => goo?.Value is not null).Select(static goo => goo!.Value!),
            headers.Where(static goo => goo?.Value is not null).Select(static goo => goo!.Value!),
            timeoutSeconds);

        return _executor.Execute(request);
    }

    protected bool ValidateUrl(string url, bool throwOnInvalid = true)
    {
        if (string.IsNullOrEmpty(url))
        {
            return InvalidUrlReturnValue(" Invalid URL.", throwOnInvalid);
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidUrlReturnValue(" URL must include a scheme (http:// or https://)", throwOnInvalid);
        }

        Uri uri = new(url);

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return InvalidUrlReturnValue(" URL is not well formed.", throwOnInvalid);
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return InvalidUrlReturnValue(" Please make sure your URL starts with 'http' or 'https'.", throwOnInvalid);
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            return InvalidUrlReturnValue(" Please do not include query parameters in your URL. Use the Params (P) input instead.", throwOnInvalid);
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            return InvalidUrlReturnValue(" Please do not include a fragment in your URL.", throwOnInvalid);
        }

        if (uri.HostNameType == UriHostNameType.Dns)
        {
            IPHostEntry? hostEntry;
            try
            {
                hostEntry = Dns.GetHostEntry(uri.Host);
            }
            catch (System.Net.Sockets.SocketException)
            {
                return InvalidUrlReturnValue(" Please use a valid hostname or IP address.", throwOnInvalid);
            }

            if (_ipBlacklist.IsHostBlacklisted(hostEntry))
            {
                return InvalidUrlReturnValue(" The given hostname or IP address is blacklisted.", throwOnInvalid);
            }
        }
        else if (uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
        {
            if (!IPAddress.TryParse(uri.Host, out IPAddress? ipAddress))
            {
                return InvalidUrlReturnValue(" Please use a valid hostname or IP address.", throwOnInvalid);
            }

            if (_ipBlacklist.IsAddressBlacklisted(ipAddress))
            {
                return InvalidUrlReturnValue(" The given hostname or IP address is blacklisted.", throwOnInvalid);
            }
        }
        else
        {
            return InvalidUrlReturnValue(" The given hostname or IP address is invalid.", throwOnInvalid);
        }

        return true;
    }

    private static bool InvalidUrlReturnValue(string message, bool throwOnInvalid)
    {
        if (throwOnInvalid)
        {
            throw new Exception(message);
        }

        return false;
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    private sealed record RequestInputs(
        string Url,
        string Method,
        IRequestBody? Body,
        List<QueryParameterGoo> QueryParameters,
        List<HttpHeaderGoo> Headers);
}

