using System.Net.Http;
using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DownloadFileComponent : GH_Component
{
    // Transfer state and worker live in Swiftlet.Runtime (shared with the GH2 shell).
    private readonly Swiftlet.Runtime.Transfers.FileDownloadJob _job = new();

    public DownloadFileComponent()
        : base("Download File", "DL", "Download a file from a URL directly to disk (streaming, memory-efficient for large files)", ShellNaming.Category, ShellNaming.Send)
    {
        _job.Changed += OnJobChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "URL to download from", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "P", "Destination file path", GH_ParamAccess.item);
        pManager.AddParameter(new QueryParameterParam(), "Params", "Q", "Query parameters", GH_ParamAccess.list);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "HTTP headers", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Run", "R", "Set to true to start download", GH_ParamAccess.item, false);
        pManager.AddBooleanParameter("Overwrite", "O", "Overwrite existing file", GH_ParamAccess.item, true);

        pManager[2].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Done", "D", "True when download is complete", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Progress", "P", "Download progress (0-100%), or -1 if server doesn't report file size", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "F", "Path to downloaded file", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Status", "S", "HTTP status code", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Size", "Sz", "File size in bytes", GH_ParamAccess.item);
        pManager.AddTextParameter("Error", "E", "Error message if download failed", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string url = string.Empty;
        string path = string.Empty;
        List<QueryParameterGoo> queryParams = [];
        List<HttpHeaderGoo> headers = [];
        bool run = false;
        bool overwrite = true;

        DA.GetData(0, ref url);
        DA.GetData(1, ref path);
        DA.GetDataList(2, queryParams);
        DA.GetDataList(3, headers);
        DA.GetData(4, ref run);
        DA.GetData(5, ref overwrite);

        var state = _job.Snapshot;
        if (!run)
        {
            _job.Reset();
            Message = "Idle";
            DA.SetData(0, false);
            DA.SetData(1, 0);
            DA.SetData(2, null);
            DA.SetData(3, 0);
            DA.SetData(4, 0);
            DA.SetData(5, null);
            return;
        }

        if (state.Completed)
        {
            DA.SetData(0, state.Success);
            DA.SetData(1, state.Progress);
            DA.SetData(2, state.Success ? state.Path : null);
            DA.SetData(3, state.StatusCode);
            DA.SetData(4, (int)state.Bytes);
            DA.SetData(5, state.Error);
            Message = state.Success ? $"Done ({TransferFormatting.FormatBytes(state.Bytes)})" : "Failed";
            return;
        }

        if (state.Running)
        {
            DA.SetData(0, false);
            DA.SetData(1, state.TotalBytes > 0 ? state.Progress : -1);
            DA.SetData(2, null);
            DA.SetData(3, 0);
            DA.SetData(4, (int)state.Bytes);
            DA.SetData(5, null);
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

        if (File.Exists(normalizedPath) && !overwrite)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File already exists and Overwrite is false");
            return;
        }

        string fullUrl = UrlBuilder.AddQueryParameters(
            url,
            queryParams.Where(static p => p?.Value is not null).Select(static p => p!.Value!));

        HttpHeader[] headerValues = headers.Where(static h => h?.Value is not null).Select(static h => h!.Value!).ToArray();
        Message = "Starting...";
        _job.Start(fullUrl, normalizedPath, headerValues);

        DA.SetData(0, false);
        DA.SetData(1, 0);
        DA.SetData(2, null);
        DA.SetData(3, 0);
        DA.SetData(4, 0);
        DA.SetData(5, null);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("D1E2F3A4-B5C6-7D8E-9F00-112233445566");

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
        string? message = completed ? null : Swiftlet.Runtime.Transfers.TransferMessages.DownloadProgress(_job.Snapshot);
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
