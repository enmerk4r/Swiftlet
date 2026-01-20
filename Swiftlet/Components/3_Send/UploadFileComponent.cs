using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
    /// Uploads a file to a URL using streaming (memory-efficient for large files).
    /// Uses async pattern to keep UI responsive during upload.
    /// </summary>
    public class UploadFileComponent : GH_Component
    {
        private const int BufferSize = 81920; // 80KB buffer

        // Async state
        private Task _uploadTask;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isUploading;
        private volatile int _progress;
        private long _bytesUploaded;
        private long _totalBytes;
        private volatile bool _completed;
        private volatile bool _success;
        private volatile string _error;
        private volatile int _statusCode;
        private volatile string _responseContent;

        // Throttle UI updates
        private DateTime _lastUiUpdate = DateTime.MinValue;
        private readonly TimeSpan _uiUpdateInterval = TimeSpan.FromMilliseconds(100);

        public UploadFileComponent()
          : base("Upload File", "UL",
              "Upload a file to a URL using streaming (memory-efficient for large files)",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "Upload endpoint URL", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "P", "Source file path", GH_ParamAccess.item);
            pManager.AddTextParameter("Method", "M", "HTTP method (POST or PUT)", GH_ParamAccess.item, "POST");
            pManager.AddTextParameter("Content Type", "T", "MIME type (auto-detected if empty)", GH_ParamAccess.item, "");
            pManager.AddParameter(new QueryParamParam(), "Params", "Q", "Query parameters", GH_ParamAccess.list);
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
            List<QueryParamGoo> queryParams = new List<QueryParamGoo>();
            List<HttpHeaderGoo> headers = new List<HttpHeaderGoo>();
            bool run = false;

            DA.GetData(0, ref url);
            DA.GetData(1, ref path);
            DA.GetData(2, ref method);
            DA.GetData(3, ref contentType);
            DA.GetDataList(4, queryParams);
            DA.GetDataList(5, headers);
            DA.GetData(6, ref run);

            // User turned off Run - cancel any ongoing upload and reset
            if (!run)
            {
                if (_isUploading)
                {
                    CancelUpload();
                }
                ResetState();
                this.Message = "Idle";
                DA.SetData(0, false);
                DA.SetData(1, 0);
                DA.SetData(2, 0);
                DA.SetData(3, null);
                DA.SetData(4, null);
                return;
            }

            // Upload completed - output results
            if (_completed)
            {
                DA.SetData(0, _success);
                DA.SetData(1, _progress);
                DA.SetData(2, _statusCode);
                DA.SetData(3, _responseContent);
                DA.SetData(4, _error);

                if (_success)
                {
                    this.Message = "Done";
                }
                else
                {
                    this.Message = "Failed";
                }
                return;
            }

            // Upload in progress - output current progress
            if (_isUploading)
            {
                DA.SetData(0, false);
                DA.SetData(1, _progress);
                DA.SetData(2, 0);
                DA.SetData(3, null);
                DA.SetData(4, null);
                return;
            }

            // Start new upload - validate inputs
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

            if (!File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File does not exist");
                return;
            }

            method = method.ToUpperInvariant();
            if (method != "POST" && method != "PUT")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Method must be POST or PUT");
                return;
            }

            // Auto-detect content type if not specified
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = MimeTypeUtility.GetMimeType(path);
            }

            // Build URL with query params
            string fullUrl = UrlUtility.AddQueryParams(url, queryParams.Select(q => q.Value).ToList());
            var headerList = headers.Select(h => h.Value).ToList();
            long fileSize = new FileInfo(path).Length;

            // Start async upload
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
            var token = _cancellationTokenSource.Token;

            this.Message = $"Starting... ({FormatBytes(fileSize)})";

            _uploadTask = Task.Run(async () =>
            {
                await UploadAsync(fullUrl, path, method, contentType, headerList, fileSize, token);
            });

            // Output initial state
            DA.SetData(0, false);
            DA.SetData(1, 0);
            DA.SetData(2, 0);
            DA.SetData(3, null);
            DA.SetData(4, null);
        }

        private async Task UploadAsync(string url, string path, string method, string contentType,
            List<DataModels.Implementations.HttpHeader> headers, long fileSize, CancellationToken token)
        {
            try
            {
                var httpMethod = method == "PUT" ? HttpMethod.Put : HttpMethod.Post;

                using (var request = new HttpRequestMessage(httpMethod, url))
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    // Create file stream with progress tracking
                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true))
                    {
                        var progressStream = new ProgressStream(fileStream, bytesRead =>
                        {
                            _bytesUploaded = bytesRead;
                            if (fileSize > 0)
                            {
                                int newProgress = (int)((bytesRead * 100L) / fileSize);
                                _progress = Math.Min(newProgress, 99);
                            }
                            UpdateProgressMessage();
                        });

                        var streamContent = new StreamContent(progressStream, BufferSize);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                        streamContent.Headers.ContentLength = fileSize;

                        request.Content = streamContent;

                        using (var response = await HttpClientFactory.SharedClient.SendAsync(request, token))
                        {
                            _statusCode = (int)response.StatusCode;
                            _responseContent = await response.Content.ReadAsStringAsync();
                            _success = response.IsSuccessStatusCode;
                            _progress = 100;

                            if (!_success)
                            {
                                _error = $"HTTP {_statusCode}: {response.ReasonPhrase}";
                            }
                        }
                    }
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
            var now = DateTime.Now;
            if (now - _lastUiUpdate < _uiUpdateInterval)
                return;

            _lastUiUpdate = now;

            string message;
            if (_totalBytes > 0)
            {
                message = $"Uploading... {_progress}% ({FormatBytes(_bytesUploaded)} / {FormatBytes(_totalBytes)})";
            }
            else
            {
                message = $"Uploading... {FormatBytes(_bytesUploaded)}";
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

        private void CancelUpload()
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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_upload_file;

        public override Guid ComponentGuid => new Guid("E2F3A4B5-C6D7-8E9F-0011-223344556677");
    }
}
