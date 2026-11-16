using System.Net.Http;
using System.Net.Http.Headers;
using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UploadFileComponent : GH_Component
{
    // Transfer state and worker live in Swiftlet.Runtime (shared with the GH2 shell).
    private readonly Swiftlet.Runtime.Transfers.FileUploadJob _job = new();

    public UploadFileComponent()
        : base("Upload File", "UL", "Upload a file to a URL using streaming (memory-efficient for large files)", ShellNaming.Category, ShellNaming.Send)
    {
        _job.Changed += OnJobChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "Upload endpoint URL", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "P", "Source file path", GH_ParamAccess.item);
        pManager.AddTextParameter("Method", "M", "HTTP method (POST or PUT)", GH_ParamAccess.item, "POST");
        pManager.AddTextParameter("Content Type", "T", "MIME type (auto-detected if empty)", GH_ParamAccess.item, string.Empty);
        pManager.AddParameter(new QueryParameterParam(), "Params", "Q", "Query parameters", GH_ParamAccess.list);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "HTTP headers", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Run", "R", "Set to true to start upload", GH_ParamAccess.item, false);

        pManager[4].Optional = true;
        pManager[5].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Done", "D", "True when upload is complete", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Progress", "P", "Upload progress (0-100%)", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Status", "S", "HTTP status code", GH_ParamAccess.item);
        pManager.AddTextParameter("Content", "C", "Response body", GH_ParamAccess.item);
        pManager.AddTextParameter("Error", "E", "Error message if upload failed", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string url = string.Empty;
        string path = string.Empty;
        string method = "POST";
        string contentType = string.Empty;
        List<QueryParameterGoo> queryParams = [];
        List<HttpHeaderGoo> headers = [];
        bool run = false;

        DA.GetData(0, ref url);
        DA.GetData(1, ref path);
        DA.GetData(2, ref method);
        DA.GetData(3, ref contentType);
        DA.GetDataList(4, queryParams);
        DA.GetDataList(5, headers);
        DA.GetData(6, ref run);

        var state = _job.Snapshot;
        if (!run)
        {
            _job.Reset();
            Message = "Idle";
            DA.SetData(0, false);
            DA.SetData(1, 0);
            DA.SetData(2, 0);
            DA.SetData(3, null);
            DA.SetData(4, null);
            return;
        }

        if (state.Completed)
        {
            DA.SetData(0, state.Success);
            DA.SetData(1, state.Progress);
            DA.SetData(2, state.StatusCode);
            DA.SetData(3, state.Content);
            DA.SetData(4, state.Error);
            Message = state.Success ? "Done" : "Failed";
            return;
        }

        if (state.Running)
        {
            DA.SetData(0, false);
            DA.SetData(1, state.Progress);
            DA.SetData(2, 0);
            DA.SetData(3, null);
            DA.SetData(4, null);
            return;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "URL is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is required");
            return;
        }

        string normalizedPath;
        try
        {
            normalizedPath = FilePathUtility.NormalizePath(path);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid path: {ex.Message}");
            return;
        }

        if (!File.Exists(normalizedPath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File does not exist");
            return;
        }

        method = method.ToUpperInvariant();
        if (method is not ("POST" or "PUT"))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Method must be POST or PUT");
            return;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = MimeTypeMap.GetMimeType(normalizedPath);
        }

        string fullUrl = UrlBuilder.AddQueryParameters(
            url,
            queryParams.Where(static p => p?.Value is not null).Select(static p => p!.Value!));
        HttpHeader[] headerValues = headers.Where(static h => h?.Value is not null).Select(static h => h!.Value!).ToArray();
        long fileSize = new FileInfo(normalizedPath).Length;

        Message = $"Starting... ({TransferFormatting.FormatBytes(fileSize)})";
        _job.Start(fullUrl, normalizedPath, method, contentType, headerValues, fileSize);

        DA.SetData(0, false);
        DA.SetData(1, 0);
        DA.SetData(2, 0);
        DA.SetData(3, null);
        DA.SetData(4, null);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("E2F3A4B5-C6D7-8E9F-0011-223344556677");

    public override void RemovedFromDocument(GH_Document document)
    {
        _job.Reset();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            _job.Reset();
        }

        base.DocumentContextChanged(document, context);
    }

    private void OnJobChanged(bool completed)
    {
        // Worker thread: progress updates the badge and re-solves (throttled by the job); completion re-solves.
        string? message = completed ? null : Swiftlet.Runtime.Transfers.TransferMessages.UploadProgress(_job.Snapshot);
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            if (message is not null)
            {
                Message = message;
            }

            ExpireSolution(true);
        }));
    }
}
