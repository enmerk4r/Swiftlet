using System.Net.Http;
using System.Net.Http.Headers;
using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UploadFileMultipartComponent : GH_Component
{
    // Transfer state and worker live in Swiftlet.Runtime (shared with the GH2 shell).
    private readonly Swiftlet.Runtime.Transfers.FileMultipartUploadJob _job = new();

    public UploadFileMultipartComponent()
        : base("Upload File Multipart", "ULMP", "Upload a file as multipart/form-data with optional form fields (memory-efficient for large files)", ShellNaming.Category, ShellNaming.Send)
    {
        _job.Changed += OnJobChanged;
    }

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("URL", "U", "Upload endpoint URL", GH_ParamAccess.item);
        pManager.AddTextParameter("Path", "P", "Source file path", GH_ParamAccess.item);
        pManager.AddTextParameter("Field Name", "N", "Form field name for the file", GH_ParamAccess.item, "file");
        pManager.AddTextParameter("File Name", "F", "Filename to send (defaults to actual filename)", GH_ParamAccess.item, string.Empty);
        pManager.AddTextParameter("Content Type", "T", "MIME type (auto-detected if empty)", GH_ParamAccess.item, string.Empty);
        pManager.AddParameter(new MultipartFieldParam(), "Fields", "Fd", "Additional form fields", GH_ParamAccess.list);
        pManager.AddParameter(new HttpHeaderParam(), "Headers", "H", "HTTP headers", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Run", "R", "Set to true to start upload", GH_ParamAccess.item, false);

        pManager[3].Optional = true;
        pManager[4].Optional = true;
        pManager[5].Optional = true;
        pManager[6].Optional = true;
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
        string fieldName = "file";
        string fileName = string.Empty;
        string contentType = string.Empty;
        List<MultipartFieldGoo> fields = [];
        List<HttpHeaderGoo> headers = [];
        bool run = false;

        DA.GetData(0, ref url);
        DA.GetData(1, ref path);
        DA.GetData(2, ref fieldName);
        DA.GetData(3, ref fileName);
        DA.GetData(4, ref contentType);
        DA.GetDataList(5, fields);
        DA.GetDataList(6, headers);
        DA.GetData(7, ref run);

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

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            fieldName = "file";
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = Path.GetFileName(normalizedPath);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = MimeTypeMap.GetMimeType(normalizedPath);
        }

        HttpHeader[] headerValues = headers.Where(static h => h?.Value is not null).Select(static h => h!.Value!).ToArray();
        MultipartField[] fieldValues = fields.Where(static f => f?.Value is not null).Select(static f => f!.Value!).ToArray();
        long fileSize = new FileInfo(normalizedPath).Length;

        Message = $"Starting... ({TransferFormatting.FormatBytes(fileSize)})";
        _job.Start(url, normalizedPath, fieldName, fileName, contentType, fieldValues, headerValues, fileSize);

        DA.SetData(0, false);
        DA.SetData(1, 0);
        DA.SetData(2, 0);
        DA.SetData(3, null);
        DA.SetData(4, null);
    }

    protected override System.Drawing.Bitmap? Icon => ShellIcons.For(GetType());

    public override Guid ComponentGuid => new("F3A4B5C6-D7E8-9F00-1122-334455667788");

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
