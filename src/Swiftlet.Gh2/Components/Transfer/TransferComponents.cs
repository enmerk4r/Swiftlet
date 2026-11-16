using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.Parameters;
using Grasshopper2.Undo;
using GrasshopperIO;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;
using Swiftlet.Runtime.Transfers;

namespace Swiftlet.Gh2.Components.Transfer;

/// <summary>
/// Shared lifecycle for the background file-transfer components, following the Server Input pattern:
/// solves are single-threaded per component, job callbacks queue one coalesced recompute on
/// the UI thread after any running solution stops (guarded by a generation counter), and
/// the transfer is cancelled when the component is removed, disabled or its document closes.
/// </summary>
public abstract class TransferComponent : SwiftletComponent, IRemovalAware
{
    /// <summary>The component's transfer; derived classes create it in a field initializer.</summary>
    private protected abstract FileTransferJob Job { get; }
    private long _generation;

    protected TransferComponent(string name, string info) : base(name, info, Sections.Send) => Initialize();

    protected TransferComponent(IReader reader) : base(reader) => Initialize();

    private void Initialize()
    {
        Threading = ThreadingState.SingleThreaded;
        Job.Changed += _ => PostExpiry();
        DocumentChanged += OnDocumentChanged;
        ActivityChanged += (_, _) =>
        {
            if (Activity == ObjectActivity.Disabled)
                Stop();
        };
    }

    public void PrepareForRemoval(ActionList undo) => Stop();

    /// <summary>GH1 "Run = false" and every teardown: cancel and forget the transfer.</summary>
    protected void Stop()
    {
        Interlocked.Increment(ref _generation);
        Job.Reset();
    }

    private void PostExpiry()
    {
        long generation = Volatile.Read(ref _generation);
        BackgroundSolveScheduler.Request(this,
            () => generation == Volatile.Read(ref _generation));
    }

    private void OnDocumentChanged(object? sender, ObjectDocumentEventArgs args)
    {
        if (args.OldDocument is { } old)
        {
            old.StateChanged -= OnDocumentStateChanged;
            Stop();
        }

        if (args.NewDocument is { } current)
            current.StateChanged += OnDocumentStateChanged;
    }

    private void OnDocumentStateChanged(object? sender, DocumentStateEventArgs args)
    {
        if (args.NewState is DocumentState.Closing or DocumentState.Closed or DocumentState.Inert)
            Stop();
    }

    /// <summary>
    /// GH1's Message for a transfer that is idle, finished or running ("Starting..." is set by the caller when
    /// it starts one). Progress solves come from <see cref="FileTransferJob.Changed"/>, so the text follows them.
    /// </summary>
    private protected void ShowTransferMessage(bool run, TransferSnapshot state, bool download)
    {
        if (!run)
            ShowMessage("Idle");
        else if (state.Completed)
            ShowMessage(!state.Success ? "Failed" : download ? $"Done ({TransferFormatting.FormatBytes(state.Bytes)})" : "Done");
        else if (state.Running)
            ShowMessage(download ? TransferMessages.DownloadProgress(state) : TransferMessages.UploadProgress(state));
    }

    /// <summary>Validates URL and path as GH1 does; returns the normalised path or null after an error.</summary>
    private protected static string? ValidateUrlAndPath(IDataAccess access, string? url, string? path)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            access.AddError("URL is required", "URL is required");
            return null;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            access.AddError("Path is required", "Path is required");
            return null;
        }

        try
        {
            return FilePathUtility.NormalizePath(path);
        }
        catch (Exception ex)
        {
            access.AddError("Invalid path", $"Invalid path: {ex.Message}");
            return null;
        }
    }

    private protected static T[] Items<T>(IDataAccess access, int index) where T : class
    {
        access.GetTwig<T>(index, out var twig);
        return twig?.NonNullItems.ToArray() ?? Array.Empty<T>();
    }

    private protected static void AddQueryAndHeaders(InputAdder inputs)
    {
        inputs.Add(new QueryParameterParameter("Params", "Q", "Query parameters", Access.Twig), Requirement.MayBeMissing);
        inputs.Add(new HttpHeaderParameter("Headers", "H", "HTTP headers", Access.Twig), Requirement.MayBeMissing);
    }
}

[IoId(ComponentIds.DownloadFile)]
public sealed class DownloadFileComponent : TransferComponent
{
    private readonly FileDownloadJob _job = new();
    private protected override FileTransferJob Job => _job;

    public DownloadFileComponent()
        : base("Download File", "Download a file from a URL directly to disk (streaming, memory-efficient for large files)") { }

    public DownloadFileComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URL", "U", "URL to download from");
        inputs.AddText("Path", "P", "Destination file path");
        AddQueryAndHeaders(inputs);
        inputs.AddBoolean("Run", "R", "Set to true to start download").Set(false);
        inputs.AddBoolean("Overwrite", "O", "Overwrite existing file").Set(true);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddBoolean("Done", "D", "True when download is complete");
        outputs.AddInteger("Progress", "P", "Download progress (0-100%), or -1 if server doesn't report file size");
        outputs.AddText("Path", "F", "Path to downloaded file");
        outputs.AddInteger("Status", "S", "HTTP status code");
        outputs.AddInteger("Size", "Sz", "File size in bytes");
        outputs.AddText("Error", "E", "Error message if download failed");
    }

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string url);
        access.GetItem(1, out string path);
        var query = Items<QueryParameter>(access, 2);
        var headers = Items<HttpHeader>(access, 3);
        access.GetItem(4, out bool run);
        access.GetItem(5, out bool overwrite);

        var state = Job.Snapshot;
        ShowTransferMessage(run, state, download: true);
        if (!run)
        {
            Stop();
            Set(access, false, 0, null, 0, 0, null);
            return;
        }

        if (state.Completed)
        {
            Set(access, state.Success, state.Progress, state.Success ? state.Path : null, state.StatusCode, (int)state.Bytes, state.Error);
            return;
        }

        if (state.Running)
        {
            Set(access, false, state.TotalBytes > 0 ? state.Progress : -1, null, 0, (int)state.Bytes, null);
            return;
        }

        string? normalizedPath = ValidateUrlAndPath(access, url, path);
        if (normalizedPath is null)
            return;
        if (File.Exists(normalizedPath) && !overwrite)
        {
            access.AddError("File exists", "File already exists and Overwrite is false");
            return;
        }

        ShowMessage("Starting...");
        _job.Start(UrlBuilder.AddQueryParameters(url, query), normalizedPath, headers);
        Set(access, false, 0, null, 0, 0, null);
    }

    private static void Set(IDataAccess access, bool done, int progress, string? path, int status, int size, string? error)
    {
        access.SetItem(0, done);
        access.SetItem(1, progress);
        access.SetItem(2, path);
        access.SetItem(3, status);
        access.SetItem(4, size);
        access.SetItem(5, error);
    }
}

[IoId(ComponentIds.UploadFile)]
public sealed class UploadFileComponent : TransferComponent
{
    private readonly FileUploadJob _job = new();
    private protected override FileTransferJob Job => _job;

    public UploadFileComponent()
        : base("Upload File", "Upload a file to a URL using streaming (memory-efficient for large files)") { }

    public UploadFileComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URL", "U", "Upload endpoint URL");
        inputs.AddText("Path", "P", "Source file path");
        inputs.AddText("Method", "M", "HTTP method (POST or PUT)").Set("POST");
        inputs.AddText("Content Type", "T", "MIME type (auto-detected if empty)").Set(string.Empty);
        AddQueryAndHeaders(inputs);
        inputs.AddBoolean("Run", "R", "Set to true to start upload").Set(false);
    }

    protected override void AddOutputs(OutputAdder outputs) => UploadOutputs.Add(outputs);

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string url);
        access.GetItem(1, out string path);
        access.GetItem(2, out string method);
        access.GetItem(3, out string contentType);
        var query = Items<QueryParameter>(access, 4);
        var headers = Items<HttpHeader>(access, 5);
        access.GetItem(6, out bool run);

        var state = Job.Snapshot;
        ShowTransferMessage(run, state, download: false);
        if (UploadOutputs.Report(access, run, state, Stop))
            return;

        string? normalizedPath = ValidateUrlAndPath(access, url, path);
        if (normalizedPath is null)
            return;
        if (!File.Exists(normalizedPath))
        {
            access.AddError("File does not exist", "File does not exist");
            return;
        }

        method = (method ?? string.Empty).ToUpperInvariant();
        if (method is not ("POST" or "PUT"))
        {
            access.AddError("Invalid method", "Method must be POST or PUT");
            return;
        }

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = MimeTypeMap.GetMimeType(normalizedPath);

        long fileSize = new FileInfo(normalizedPath).Length;
        ShowMessage($"Starting... ({TransferFormatting.FormatBytes(fileSize)})");
        _job.Start(UrlBuilder.AddQueryParameters(url, query), normalizedPath, method, contentType, headers, fileSize);
        UploadOutputs.Set(access, false, 0, 0, null, null);
    }
}

[IoId(ComponentIds.UploadFileMultipart)]
public sealed class UploadFileMultipartComponent : TransferComponent
{
    private readonly FileMultipartUploadJob _job = new();
    private protected override FileTransferJob Job => _job;

    public UploadFileMultipartComponent()
        : base("Upload File Multipart", "Upload a file as multipart/form-data with optional form fields (memory-efficient for large files)") { }

    public UploadFileMultipartComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("URL", "U", "Upload endpoint URL");
        inputs.AddText("Path", "P", "Source file path");
        inputs.AddText("Field Name", "N", "Form field name for the file").Set("file");
        inputs.AddText("File Name", "F", "Filename to send (defaults to actual filename)", Access.Item, Requirement.MayBeMissing).Set(string.Empty);
        inputs.AddText("Content Type", "T", "MIME type (auto-detected if empty)", Access.Item, Requirement.MayBeMissing).Set(string.Empty);
        inputs.Add(new MultipartFieldParameter("Fields", "Fd", "Additional form fields", Access.Twig), Requirement.MayBeMissing);
        inputs.Add(new HttpHeaderParameter("Headers", "H", "HTTP headers", Access.Twig), Requirement.MayBeMissing);
        inputs.AddBoolean("Run", "R", "Set to true to start upload").Set(false);
    }

    protected override void AddOutputs(OutputAdder outputs) => UploadOutputs.Add(outputs);

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string url);
        access.GetItem(1, out string path);
        access.GetItem(2, out string fieldName);
        if (!access.GetItem(3, out string fileName)) fileName = string.Empty;
        if (!access.GetItem(4, out string contentType)) contentType = string.Empty;
        var fields = Items<MultipartField>(access, 5);
        var headers = Items<HttpHeader>(access, 6);
        access.GetItem(7, out bool run);

        var state = Job.Snapshot;
        ShowTransferMessage(run, state, download: false);
        if (UploadOutputs.Report(access, run, state, Stop))
            return;

        string? normalizedPath = ValidateUrlAndPath(access, url, path);
        if (normalizedPath is null)
            return;
        if (!File.Exists(normalizedPath))
        {
            access.AddError("File does not exist", "File does not exist");
            return;
        }

        if (string.IsNullOrWhiteSpace(fieldName))
            fieldName = "file";
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = System.IO.Path.GetFileName(normalizedPath);
        if (string.IsNullOrWhiteSpace(contentType))
            contentType = MimeTypeMap.GetMimeType(normalizedPath);

        long fileSize = new FileInfo(normalizedPath).Length;
        ShowMessage($"Starting... ({TransferFormatting.FormatBytes(fileSize)})");
        _job.Start(url, normalizedPath, fieldName, fileName, contentType, fields, headers, fileSize);
        UploadOutputs.Set(access, false, 0, 0, null, null);
    }
}

/// <summary>The five outputs shared by both upload components and their idle/running/completed states.</summary>
internal static class UploadOutputs
{
    public static void Add(OutputAdder outputs)
    {
        outputs.AddBoolean("Done", "D", "True when upload is complete");
        outputs.AddInteger("Progress", "P", "Upload progress (0-100%)");
        outputs.AddInteger("Status", "S", "HTTP status code");
        outputs.AddText("Content", "C", "Response body");
        outputs.AddText("Error", "E", "Error message if upload failed");
    }

    /// <summary>Handles Run = false, a completed and a running upload; false means "start one".</summary>
    public static bool Report(IDataAccess access, bool run, TransferSnapshot state, System.Action stop)
    {
        if (!run)
        {
            stop();
            Set(access, false, 0, 0, null, null);
            return true;
        }

        if (state.Completed)
        {
            Set(access, state.Success, state.Progress, state.StatusCode, state.Content, state.Error);
            return true;
        }

        if (state.Running)
        {
            Set(access, false, state.Progress, 0, null, null);
            return true;
        }

        return false;
    }

    public static void Set(IDataAccess access, bool done, int progress, int status, string? content, string? error)
    {
        access.SetItem(0, done);
        access.SetItem(1, progress);
        access.SetItem(2, status);
        access.SetItem(3, content);
        access.SetItem(4, error);
    }
}
