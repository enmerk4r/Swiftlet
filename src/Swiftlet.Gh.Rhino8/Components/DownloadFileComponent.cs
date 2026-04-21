using System.Net.Http;
using Grasshopper.Kernel;
using Swiftlet.Core.Http;
using Swiftlet.Gh.Rhino8.Goo;
using Swiftlet.Gh.Rhino8.Params;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed class DownloadFileComponent : GH_Component
{
    private const int BufferSize = 81920;
    private static readonly HttpClient HttpClient = new();

    private Task? _downloadTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isDownloading;
    private volatile int _progress;
    private long _bytesDownloaded;
    private long _totalBytes;
    private volatile bool _completed;
    private volatile bool _success;
    private string? _error;
    private volatile int _statusCode;
    private string? _downloadPath;
    private DateTime _lastUiUpdate = DateTime.MinValue;
    private readonly TimeSpan _uiUpdateInterval = TimeSpan.FromMilliseconds(100);

    public DownloadFileComponent()
        : base("Download File", "DL", "Download a file from a URL directly to disk (streaming, memory-efficient for large files)", ShellNaming.Category, ShellNaming.Send)
    {
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

        if (!run)
        {
            if (_isDownloading)
            {
                CancelDownload();
            }

            ResetState();
            Message = "Idle";
            DA.SetData(0, false);
            DA.SetData(1, 0);
            DA.SetData(2, null);
            DA.SetData(3, 0);
            DA.SetData(4, 0);
            DA.SetData(5, null);
            return;
        }

        if (_completed)
        {
            DA.SetData(0, _success);
            DA.SetData(1, _progress);
            DA.SetData(2, _success ? _downloadPath : null);
            DA.SetData(3, _statusCode);
            DA.SetData(4, (int)_bytesDownloaded);
            DA.SetData(5, _error);
            Message = _success ? $"Done ({TransferFormatting.FormatBytes(_bytesDownloaded)})" : "Failed";
            return;
        }

        if (_isDownloading)
        {
            DA.SetData(0, false);
            DA.SetData(1, _totalBytes > 0 ? _progress : -1);
            DA.SetData(2, null);
            DA.SetData(3, 0);
            DA.SetData(4, (int)_bytesDownloaded);
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

        if (File.Exists(path) && !overwrite)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File already exists and Overwrite is false");
            return;
        }

        string fullUrl = UrlBuilder.AddQueryParameters(
            url,
            queryParams.Where(static p => p?.Value is not null).Select(static p => p!.Value!));

        _downloadPath = path;
        _isDownloading = true;
        _completed = false;
        _success = false;
        _progress = 0;
        _bytesDownloaded = 0;
        _totalBytes = 0;
        _error = null;
        _statusCode = 0;
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _cancellationTokenSource.Token;

        HttpHeader[] headerValues = headers.Where(static h => h?.Value is not null).Select(static h => h!.Value!).ToArray();
        Message = "Starting...";
        _downloadTask = Task.Run(() => DownloadAsync(fullUrl, path, headerValues, token), token);

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
        CancelDownload();
        ResetState();
        base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        if (context == GH_DocumentContext.Close)
        {
            CancelDownload();
            ResetState();
        }

        base.DocumentContextChanged(document, context);
    }

    private async Task DownloadAsync(string url, string path, IReadOnlyList<HttpHeader> headers, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (HttpHeader header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using HttpResponseMessage response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            _statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                _error = $"HTTP {_statusCode}: {response.ReasonPhrase}";
                _completed = true;
                _isDownloading = false;
                ScheduleUiUpdate();
                return;
            }

            _totalBytes = response.Content.Headers.ContentLength ?? 0;
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            byte[] buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token).ConfigureAwait(false);
                _bytesDownloaded += bytesRead;
                if (_totalBytes > 0)
                {
                    _progress = Math.Min((int)((_bytesDownloaded * 100L) / _totalBytes), 99);
                }

                UpdateProgressMessage();
            }

            _progress = 100;
            _success = true;
            _completed = true;
            _isDownloading = false;
            ScheduleUiUpdate();
        }
        catch (OperationCanceledException)
        {
            _error = "Download cancelled";
            _completed = true;
            _isDownloading = false;
            TryDeleteFile(path);
            ScheduleUiUpdate();
        }
        catch (HttpRequestException ex)
        {
            _error = $"HTTP error: {ex.Message}";
            _completed = true;
            _isDownloading = false;
            TryDeleteFile(path);
            ScheduleUiUpdate();
        }
        catch (IOException ex)
        {
            _error = $"File error: {ex.Message}";
            _completed = true;
            _isDownloading = false;
            TryDeleteFile(path);
            ScheduleUiUpdate();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _completed = true;
            _isDownloading = false;
            TryDeleteFile(path);
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
        string message = _totalBytes > 0
            ? $"Downloading... {_progress}% ({TransferFormatting.FormatBytes(_bytesDownloaded)} / {TransferFormatting.FormatBytes(_totalBytes)})"
            : $"Downloading... {TransferFormatting.FormatBytes(_bytesDownloaded)}";

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

    private void CancelDownload()
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
        _isDownloading = false;
        _completed = false;
        _success = false;
        _progress = 0;
        _bytesDownloaded = 0;
        _totalBytes = 0;
        _error = null;
        _statusCode = 0;
        _downloadPath = null;
        _downloadTask = null;

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}

