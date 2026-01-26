using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Swiftlet.Goo;
using Swiftlet.Params;
using Swiftlet.Util;

namespace Swiftlet.Components
{
    /// <summary>
    /// Downloads a file from a URL directly to disk using streaming (memory-efficient for large files).
    /// Uses async pattern to keep UI responsive during download.
    /// </summary>
    public class DownloadFileComponent : GH_Component
    {
        private const int BufferSize = 81920; // 80KB buffer

        // Async state
        private Task _downloadTask;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isDownloading;
        private volatile int _progress;
        private long _bytesDownloaded;
        private long _totalBytes;
        private volatile bool _completed;
        private volatile bool _success;
        private volatile string _error;
        private volatile int _statusCode;
        private string _downloadPath;

        // Throttle UI updates
        private DateTime _lastUiUpdate = DateTime.MinValue;
        private readonly TimeSpan _uiUpdateInterval = TimeSpan.FromMilliseconds(100);

        public DownloadFileComponent()
          : base("Download File", "DL",
              "Download a file from a URL directly to disk (streaming, memory-efficient for large files)",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "URL to download from", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "P", "Destination file path", GH_ParamAccess.item);
            pManager.AddParameter(new QueryParamParam(), "Params", "Q", "Query parameters", GH_ParamAccess.list);
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
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            List<HttpHeaderGoo> headers = new List<HttpHeaderGoo>();
            bool run = false;
            bool overwrite = true;

            DA.GetData(0, ref url);
            DA.GetData(1, ref path);
            DA.GetDataList(2, queryParams);
            DA.GetDataList(3, headers);
            DA.GetData(4, ref run);
            DA.GetData(5, ref overwrite);

            // User turned off Run - cancel any ongoing download and reset
            if (!run)
            {
                if (_isDownloading)
                {
                    CancelDownload();
                }
                ResetState();
                this.Message = "Idle";
                DA.SetData(0, false);
                DA.SetData(1, 0);
                DA.SetData(2, null);
                DA.SetData(3, 0);
                DA.SetData(4, 0);
                DA.SetData(5, null);
                return;
            }

            // Download completed - output results
            if (_completed)
            {
                DA.SetData(0, _success);
                DA.SetData(1, _progress);
                DA.SetData(2, _success ? _downloadPath : null);
                DA.SetData(3, _statusCode);
                DA.SetData(4, (int)_bytesDownloaded);
                DA.SetData(5, _error);

                if (_success)
                {
                    this.Message = $"Done ({FormatBytes(_bytesDownloaded)})";
                }
                else
                {
                    this.Message = "Failed";
                }
                return;
            }

            // Download in progress - output current progress
            if (_isDownloading)
            {
                DA.SetData(0, false);
                // Output -1 if we don't know total size (no Content-Length header)
                DA.SetData(1, _totalBytes > 0 ? _progress : -1);
                DA.SetData(2, null);
                DA.SetData(3, 0);
                DA.SetData(4, (int)_bytesDownloaded);
                DA.SetData(5, null);

                // Message is updated by the download task
                return;
            }

            // Start new download
            if (string.IsNullOrEmpty(url))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "URL is required");
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is required");
                return;
            }

            if (File.Exists(path) && !overwrite)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File already exists and Overwrite is false");
                return;
            }

            // Build URL with query params
            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(q => q.Value).ToList());
            var headerList = headers.Select(h => h.Value).ToList();

            // Start async download
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
            var token = _cancellationTokenSource.Token;

            this.Message = "Starting...";

            _downloadTask = Task.Run(async () =>
            {
                await DownloadAsync(fullUrl, path, headerList, token);
            });

            // Output initial state
            DA.SetData(0, false);
            DA.SetData(1, 0);
            DA.SetData(2, null);
            DA.SetData(3, 0);
            DA.SetData(4, 0);
            DA.SetData(5, null);
        }

        private async Task DownloadAsync(string url, string path, List<DataModels.Implementations.HttpHeader> headers, CancellationToken token)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    using (var response = await HttpClientFactory.SharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token))
                    {
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

                        var directory = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                        {
                            var buffer = new byte[BufferSize];
                            int bytesRead;

                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                                _bytesDownloaded += bytesRead;

                                // Calculate progress percentage if we know total size
                                if (_totalBytes > 0)
                                {
                                    int newProgress = (int)((_bytesDownloaded * 100L) / _totalBytes);
                                    _progress = Math.Min(newProgress, 99); // Cap at 99 until complete
                                }

                                UpdateProgressMessage();
                            }
                        }

                        _progress = 100;
                        _success = true;
                        _completed = true;
                        _isDownloading = false;
                        ScheduleUiUpdate();
                    }
                }
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
            var now = DateTime.Now;
            if (now - _lastUiUpdate < _uiUpdateInterval)
                return;

            _lastUiUpdate = now;

            string message;
            if (_totalBytes > 0)
            {
                message = $"Downloading... {_progress}% ({FormatBytes(_bytesDownloaded)} / {FormatBytes(_totalBytes)})";
            }
            else
            {
                message = $"Downloading... {FormatBytes(_bytesDownloaded)}";
            }

            Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                this.Message = message;
                ExpireSolution(true);
            }));
        }

        private void ScheduleUiUpdate()
        {
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                ExpireSolution(true);
            }));
        }

        private void CancelDownload()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch
            {
                // Ignore cancellation errors
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

            try
            {
                _cancellationTokenSource?.Dispose();
            }
            catch { }
            _cancellationTokenSource = null;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:F1} {sizes[order]}";
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_download_file;

        public override Guid ComponentGuid => new Guid("D1E2F3A4-B5C6-7D8E-9F00-112233445566");
    }
}
