using System.Net.Http;
using System.Net.Http.Headers;
using Swiftlet.Core.Http;

namespace Swiftlet.Runtime.Transfers;

/// <summary>Immutable view of a transfer, read by the component on each solve.</summary>
internal sealed record TransferSnapshot(
    bool Running,
    bool Completed,
    bool Success,
    int Progress,
    long Bytes,
    long TotalBytes,
    int StatusCode,
    string? Error,
    string? Content,
    string? Path)
{
    public static readonly TransferSnapshot Idle = new(false, false, false, 0, 0, 0, 0, null, null, null);
}

/// <summary>
/// A background file transfer owned by one Download/Upload component. Moved from the GH1 components so
/// both shells run the same transfer code. The job never touches Grasshopper: it raises
/// <see cref="Changed"/> from a worker thread (progress throttled to 100 ms, completion always) and the
/// shell expires its component on the UI thread.
/// </summary>
internal abstract class FileTransferJob : IDisposable
{
    protected const int BufferSize = 81920;
    protected static readonly HttpClient Http = new();
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    private readonly object _sync = new();
    private TransferSnapshot _state = TransferSnapshot.Idle;
    private CancellationTokenSource? _cancellation;
    private DateTime _lastProgress = DateTime.MinValue;

    /// <summary>Raised on a worker thread; the argument is true for completion, false for progress.</summary>
    public event Action<bool>? Changed;

    public TransferSnapshot Snapshot
    {
        get
        {
            lock (_sync)
                return _state;
        }
    }

    /// <summary>Cancels a running transfer and returns to idle (GH1: Run set to false).</summary>
    public void Reset()
    {
        CancellationTokenSource? cancellation;
        lock (_sync)
        {
            cancellation = _cancellation;
            _cancellation = null;
            _state = TransferSnapshot.Idle;
        }

        Cancel(cancellation);
        cancellation?.Dispose();
    }

    /// <summary>Cancels a running transfer without resetting its state (GH1: removed from the document).</summary>
    public void Cancel()
    {
        CancellationTokenSource? cancellation;
        lock (_sync)
            cancellation = _cancellation;
        Cancel(cancellation);
    }

    public void Dispose() => Reset();

    protected void Begin(long totalBytes, string? path, Func<CancellationToken, Task> work)
    {
        var cancellation = new CancellationTokenSource();
        lock (_sync)
        {
            _cancellation?.Dispose();
            _cancellation = cancellation;
            _state = new TransferSnapshot(true, false, false, 0, 0, totalBytes, 0, null, null, path);
            _lastProgress = DateTime.MinValue;
        }

        CancellationToken token = cancellation.Token;
        _ = Task.Run(() => work(token), token);
    }

    /// <summary>Records progress; percentages stay below 100 until the transfer completes, as in GH1.</summary>
    protected void ReportProgress(long bytes, long totalBytes)
    {
        bool raise;
        lock (_sync)
        {
            if (!_state.Running)
                return;
            int progress = totalBytes > 0 ? Math.Min((int)(bytes * 100L / totalBytes), 99) : _state.Progress;
            _state = _state with { Bytes = bytes, TotalBytes = totalBytes, Progress = progress };
            DateTime now = DateTime.Now;
            raise = now - _lastProgress >= ProgressInterval;
            if (raise)
                _lastProgress = now;
        }

        if (raise)
            Changed?.Invoke(false);
    }

    protected void SetStatus(int statusCode)
    {
        lock (_sync)
            _state = _state with { StatusCode = statusCode };
    }

    protected void Complete(bool success, string? error, string? content = null, bool setProgress = false)
    {
        lock (_sync)
        {
            if (!_state.Running)
                return; // reset meanwhile
            _state = _state with
            {
                Running = false,
                Completed = true,
                Success = success,
                Error = error,
                Content = content,
                Progress = setProgress ? 100 : _state.Progress,
            };
        }

        Changed?.Invoke(true);
    }

    /// <summary>Maps exceptions to the GH1 error texts.</summary>
    protected static string Describe(Exception error, string verb) => error switch
    {
        OperationCanceledException => $"{verb} cancelled",
        HttpRequestException http => $"HTTP error: {http.Message}",
        IOException io => $"File error: {io.Message}",
        _ => error.Message,
    };

    protected static void AddHeaders(HttpRequestMessage request, IEnumerable<HttpHeader> headers)
    {
        foreach (HttpHeader header in headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    private static void Cancel(CancellationTokenSource? cancellation)
    {
        try
        {
            cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}

/// <summary>Download File: streams a GET response to disk.</summary>
internal sealed class FileDownloadJob : FileTransferJob
{
    public void Start(string url, string path, IReadOnlyList<HttpHeader> headers)
        => Begin(0, path, token => RunAsync(url, path, headers, token));

    private async Task RunAsync(string url, string path, IReadOnlyList<HttpHeader> headers, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, headers);
            using HttpResponseMessage response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            SetStatus(status);
            if (!response.IsSuccessStatusCode)
            {
                Complete(false, $"HTTP {status}: {response.ReasonPhrase}");
                return;
            }

            long total = response.Content.Headers.ContentLength ?? 0;
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            await using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                byte[] buffer = new byte[BufferSize];
                long written = 0;
                int read;
                ReportProgress(0, total);
                while ((read = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                    written += read;
                    ReportProgress(written, total);
                }
            }

            Complete(true, null, setProgress: true);
        }
        catch (Exception error)
        {
            TryDelete(path);
            Complete(false, Describe(error, "Download"));
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best effort, as in GH1
        }
    }
}

/// <summary>Upload File: streams a file as the body of a POST or PUT.</summary>
internal sealed class FileUploadJob : FileTransferJob
{
    public void Start(string url, string path, string method, string contentType, IReadOnlyList<HttpHeader> headers, long fileSize)
        => Begin(fileSize, null, token => RunAsync(url, path, method, contentType, headers, fileSize, token));

    private async Task RunAsync(string url, string path, string method, string contentType, IReadOnlyList<HttpHeader> headers, long fileSize, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(method == "PUT" ? HttpMethod.Put : HttpMethod.Post, url);
            AddHeaders(request, headers);
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            using var progressStream = new ProgressReadStream(fileStream, read => ReportProgress(read, fileSize));
            using var content = new StreamContent(progressStream, BufferSize);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Headers.ContentLength = fileSize;
            request.Content = content;

            using HttpResponseMessage response = await Http.SendAsync(request, token).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            SetStatus(status);
            string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            bool success = response.IsSuccessStatusCode;
            Complete(success, success ? null : $"HTTP {status}: {response.ReasonPhrase}", body, setProgress: true);
        }
        catch (Exception error)
        {
            Complete(false, Describe(error, "Upload"));
        }
    }
}

/// <summary>Upload File Multipart: streams a file plus extra fields as multipart/form-data.</summary>
internal sealed class FileMultipartUploadJob : FileTransferJob
{
    public void Start(string url, string path, string fieldName, string fileName, string contentType,
        IReadOnlyList<MultipartField> fields, IReadOnlyList<HttpHeader> headers, long fileSize)
        => Begin(fileSize, null, token => RunAsync(url, path, fieldName, fileName, contentType, fields, headers, fileSize, token));

    private async Task RunAsync(string url, string path, string fieldName, string fileName, string contentType,
        IReadOnlyList<MultipartField> fields, IReadOnlyList<HttpHeader> headers, long fileSize, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            AddHeaders(request, headers);
            using var multipart = new MultipartFormDataContent();
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            using var progressStream = new ProgressReadStream(fileStream, read => ReportProgress(read, fileSize));
            using var fileContent = new StreamContent(progressStream, BufferSize);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            multipart.Add(fileContent, fieldName, fileName);

            foreach (MultipartField field in fields)
            {
                if (!field.IsText && field.Bytes is not null)
                {
                    var bytes = new ByteArrayContent(field.Bytes);
                    if (!string.IsNullOrEmpty(field.ContentType))
                        bytes.Headers.ContentType = new MediaTypeHeaderValue(field.ContentType);
                    multipart.Add(bytes, field.Name ?? "file", field.FileName ?? "file");
                }
                else
                {
                    var text = new StringContent(field.Text ?? string.Empty);
                    if (!string.IsNullOrEmpty(field.ContentType))
                        text.Headers.ContentType = new MediaTypeHeaderValue(field.ContentType);
                    multipart.Add(text, field.Name ?? "field");
                }
            }

            request.Content = multipart;
            using HttpResponseMessage response = await Http.SendAsync(request, token).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            SetStatus(status);
            string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            bool success = response.IsSuccessStatusCode;
            Complete(success, success ? null : $"HTTP {status}: {response.ReasonPhrase}", body, setProgress: true);
        }
        catch (Exception error)
        {
            Complete(false, Describe(error, "Upload"));
        }
    }
}

/// <summary>The component badge texts GH1 shows (shared so both shells match).</summary>
internal static class TransferMessages
{
    public static string DownloadProgress(TransferSnapshot s) => s.TotalBytes > 0
        ? $"Downloading... {s.Progress}% ({TransferFormatting.FormatBytes(s.Bytes)} / {TransferFormatting.FormatBytes(s.TotalBytes)})"
        : $"Downloading... {TransferFormatting.FormatBytes(s.Bytes)}";

    public static string UploadProgress(TransferSnapshot s)
        => $"Uploading... {s.Progress}% ({TransferFormatting.FormatBytes(s.Bytes)} / {TransferFormatting.FormatBytes(s.TotalBytes)})";
}
