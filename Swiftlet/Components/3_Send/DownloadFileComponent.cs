using System;
using System.IO;
using System.Linq;
using System.Net;
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
    /// Result object for download operations.
    /// </summary>
    public class DownloadFileResult
    {
        public bool Success { get; set; }
        public string Path { get; set; }
        public int StatusCode { get; set; }
        public long FileSize { get; set; }
        public int Progress { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Downloads a file from a URL directly to disk using streaming (memory-efficient for large files).
    /// </summary>
    public class DownloadFileComponent : GH_TaskCapableComponent<DownloadFileResult>
    {
        private const int BufferSize = 81920; // 80KB buffer
        private DateTime _lastProgressUpdate = DateTime.MinValue;
        private readonly TimeSpan _progressUpdateInterval = TimeSpan.FromMilliseconds(100);

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
            pManager.AddIntegerParameter("Progress", "P", "Download progress (0-100%)", GH_ParamAccess.item);
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

            if (!run)
            {
                this.Message = "Idle";
                DA.SetData(0, false); // Done
                DA.SetData(1, 0);     // Progress
                DA.SetData(2, null);  // Path
                DA.SetData(3, 0);     // Status
                DA.SetData(4, 0);     // Size
                DA.SetData(5, null);  // Error
                return;
            }

            if (InPreSolve)
            {
                // Validate inputs
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

                // Queue async task
                var headerList = headers.Select(h => h.Value).ToList();
                var token = CancelToken;

                this.Message = "Starting...";

                TaskList.Add(Task.Run(async () =>
                {
                    return await DownloadAsync(fullUrl, path, headerList, token);
                }, token));

                return;
            }

            // Post-solve: output results
            if (!GetSolveResults(DA, out DownloadFileResult result))
            {
                // Synchronous fallback (shouldn't normally happen with Run button)
                DA.SetData(0, false);
                DA.SetData(1, 0);
                DA.SetData(2, null);
                DA.SetData(3, 0);
                DA.SetData(4, 0);
                DA.SetData(5, "No result");
                return;
            }

            if (result != null)
            {
                DA.SetData(0, result.Success);                    // Done
                DA.SetData(1, result.Progress);                   // Progress
                DA.SetData(2, result.Success ? result.Path : null); // Path
                DA.SetData(3, result.StatusCode);                 // Status
                DA.SetData(4, (int)result.FileSize);              // Size
                DA.SetData(5, result.Error);                      // Error

                if (result.Success)
                {
                    this.Message = $"Done ({FormatBytes(result.FileSize)})";
                }
                else
                {
                    this.Message = "Failed";
                }
            }
        }

        private async Task<DownloadFileResult> DownloadAsync(string url, string path, List<DataModels.Implementations.HttpHeader> headers, CancellationToken token)
        {
            var result = new DownloadFileResult { Path = path, Progress = 0 };

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    // Add headers
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    // Use ResponseHeadersRead to start streaming immediately without buffering
                    using (var response = await HttpClientFactory.SharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token))
                    {
                        result.StatusCode = (int)response.StatusCode;

                        if (!response.IsSuccessStatusCode)
                        {
                            result.Success = false;
                            result.Error = $"HTTP {result.StatusCode}: {response.ReasonPhrase}";
                            return result;
                        }

                        var contentLength = response.Content.Headers.ContentLength;
                        long totalBytes = contentLength ?? 0;
                        long bytesDownloaded = 0;

                        // Ensure directory exists
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
                                bytesDownloaded += bytesRead;

                                // Update progress
                                if (totalBytes > 0)
                                {
                                    result.Progress = (int)((bytesDownloaded * 100) / totalBytes);
                                }

                                UpdateProgressMessage(bytesDownloaded, totalBytes);
                            }
                        }

                        result.FileSize = bytesDownloaded;
                        result.Progress = 100;
                        result.Success = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Error = "Download cancelled";
                // Clean up partial file
                TryDeleteFile(path);
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.Error = $"HTTP error: {ex.Message}";
                TryDeleteFile(path);
            }
            catch (IOException ex)
            {
                result.Success = false;
                result.Error = $"File error: {ex.Message}";
                TryDeleteFile(path);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                TryDeleteFile(path);
            }

            return result;
        }

        private void UpdateProgressMessage(long bytesDownloaded, long totalBytes)
        {
            var now = DateTime.Now;
            if (now - _lastProgressUpdate < _progressUpdateInterval)
                return;

            _lastProgressUpdate = now;

            string message;
            if (totalBytes > 0)
            {
                int percent = (int)((bytesDownloaded * 100) / totalBytes);
                message = $"Downloading... {percent}% ({FormatBytes(bytesDownloaded)} / {FormatBytes(totalBytes)})";
            }
            else
            {
                message = $"Downloading... {FormatBytes(bytesDownloaded)}";
            }

            Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                this.Message = message;
            }));
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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_download_file; // TODO: Add icon

        public override Guid ComponentGuid => new Guid("D1E2F3A4-B5C6-7D8E-9F00-112233445566");
    }
}
