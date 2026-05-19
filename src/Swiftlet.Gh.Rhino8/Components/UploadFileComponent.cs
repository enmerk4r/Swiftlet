using System.Net.Http;
using System.Net.Http.Headers;
using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class UploadFileComponent : GH_Component
{
    private const int BufferSize = 81920;
    private static readonly HttpClient HttpClient = new();

    private Task? _uploadTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isUploading;
    private volatile int _progress;
    private long _bytesUploaded;
    private long _totalBytes;
    private volatile bool _completed;
    private volatile bool _success;
    private string? _error;
    private volatile int _statusCode;
    private string? _responseContent;
    private DateTime _lastUiUpdate = DateTime.MinValue;
    private readonly TimeSpan _uiUpdateInterval = TimeSpan.FromMilliseconds(100);

    public UploadFileComponent()
        : base("Upload File", "UL", "Upload a file to a URL using streaming (memory-efficient for large files)", ShellNaming.Category, ShellNaming.Send)
    {
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

        if (!run)
        {
            if (_isUploading)
            {
                CancelUpload();
            }

            ResetState();
            Message = "Idle";
            DA.SetData(0, false);
            DA.SetData(1, 0);
            DA.SetData(2, 0);
            DA.SetData(3, null);
            DA.SetData(4, null);
            return;
        }

        if (_completed)
        {
            DA.SetData(0, _success);
            DA.SetData(1, _progress);
            DA.SetData(2, _statusCode);
            DA.SetData(3, _responseContent);
            DA.SetData(4, _error);
            Message = _success ? "Done" : "Failed";
            return;
        }

        if (_isUploading)
        {
            DA.SetData(0, false);
            DA.SetData(1, _progress);
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

        if (!File.Exists(path))
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
            contentType = MimeTypeMap.GetMimeType(path);
        }

        string fullUrl = UrlBuilder.AddQueryParameters(
            url,
            queryParams.Where(static p => p?.Value is not null).Select(static p => p!.Value!));
        HttpHeader[] headerValues = headers.Where(static h => h?.Value is not null).Select(static h => h!.Value!).ToArray();
        long fileSize = new FileInfo(path).Length;

        _isUploading = true;
        _completed = false;
        _success = false;
        _progress = 0;
        _bytesUploaded = 0;
        _totalBytes = fileSize;
        _error = null;
        _statusCode = 0;
        _responseContent = null;
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token;

        Message = $"Starting... ({TransferFormatting.FormatBytes(fileSize)})";
        _uploadTask = Task.Run(() => UploadAsync(fullUrl, path, method, contentType, headerValues, fileSize, token), token);

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
        CancelUpload();
        ResetState();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            CancelUpload();
            ResetState();
        }

        base.DocumentContextChanged(document, context);
    }

    private async Task UploadAsync(
        string url,
        string path,
        string method,
        string contentType,
        IReadOnlyList<HttpHeader> headers,
        long fileSize,
        CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(method == "PUT" ? HttpMethod.Put : HttpMethod.Post, url);
            foreach (HttpHeader header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            using var progressStream = new ProgressReadStream(fileStream, bytesRead =>
            {
                _bytesUploaded = bytesRead;
                if (fileSize > 0)
                {
                    _progress = Math.Min((int)((bytesRead * 100L) / fileSize), 99);
                }

                UpdateProgressMessage();
            });

            using var streamContent = new StreamContent(progressStream, BufferSize);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            streamContent.Headers.ContentLength = fileSize;
            request.Content = streamContent;

            using HttpResponseMessage response = await HttpClient.SendAsync(request, token).ConfigureAwait(false);
            _statusCode = (int)response.StatusCode;
            _responseContent = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            _success = response.IsSuccessStatusCode;
            _progress = 100;
            if (!_success)
            {
                _error = $"HTTP {_statusCode}: {response.ReasonPhrase}";
            }

            _completed = true;
            _isUploading = false;
            ScheduleUiUpdate();
        }
        catch (OperationCanceledException)
        {
            _error = "Upload cancelled";
            _completed = true;
            _isUploading = false;
            ScheduleUiUpdate();
        }
        catch (HttpRequestException ex)
        {
            _error = $"HTTP error: {ex.Message}";
            _completed = true;
            _isUploading = false;
            ScheduleUiUpdate();
        }
        catch (IOException ex)
        {
            _error = $"File error: {ex.Message}";
            _completed = true;
            _isUploading = false;
            ScheduleUiUpdate();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _completed = true;
            _isUploading = false;
            ScheduleUiUpdate();
        }
    }

    private void UpdateProgressMessage()
    {
        DateTime now = DateTime.Now;
        if (now - _lastUiUpdate < _uiUpdateInterval)
        {
            return;
        }

        _lastUiUpdate = now;
        string message = $"Uploading... {_progress}% ({TransferFormatting.FormatBytes(_bytesUploaded)} / {TransferFormatting.FormatBytes(_totalBytes)})";
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            Message = message;
            ExpireSolution(true);
        }));
    }

    private void ScheduleUiUpdate()
    {
        Rhino.RhinoApp.InvokeOnUiThread((Action)(() => ExpireSolution(true)));
    }

    private void CancelUpload()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch
        {
        }
    }

    private void ResetState()
    {
        _isUploading = false;
        _completed = false;
        _success = false;
        _progress = 0;
        _bytesUploaded = 0;
        _totalBytes = 0;
        _error = null;
        _statusCode = 0;
        _responseContent = null;
        _uploadTask = null;

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}

