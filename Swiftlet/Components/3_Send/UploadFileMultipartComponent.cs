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
    /// Uploads a file as multipart/form-data with optional additional fields (memory-efficient for large files).
    /// </summary>
    public class UploadFileMultipartComponent : GH_TaskCapableComponent<UploadFileResult>
    {
        private const int BufferSize = 81920; // 80KB buffer
        private DateTime _lastProgressUpdate = DateTime.MinValue;
        private readonly TimeSpan _progressUpdateInterval = TimeSpan.FromMilliseconds(100);

        public UploadFileMultipartComponent()
          : base("Upload File Multipart", "ULMP",
              "Upload a file as multipart/form-data with optional form fields (memory-efficient for large files)",
              NamingUtility.CATEGORY, NamingUtility.SEND)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "Upload endpoint URL", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "P", "Source file path", GH_ParamAccess.item);
            pManager.AddTextParameter("Field Name", "N", "Form field name for the file", GH_ParamAccess.item, "file");
            pManager.AddTextParameter("File Name", "F", "Filename to send (defaults to actual filename)", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Content Type", "T", "MIME type (auto-detected if empty)", GH_ParamAccess.item, "");
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
            List<MultipartFieldGoo> fields = new List<MultipartFieldGoo>();
            List<HttpHeaderGoo> headers = new List<HttpHeaderGoo>();
            bool run = false;

            DA.GetData(0, ref url);
            DA.GetData(1, ref path);
            DA.GetData(2, ref fieldName);
            DA.GetData(3, ref fileName);
            DA.GetData(4, ref contentType);
            DA.GetDataList(5, fields);
            DA.GetDataList(6, headers);
            DA.GetData(7, ref run);

            if (!run)
            {
                this.Message = "Idle";
                DA.SetData(0, false); // Done
                DA.SetData(1, 0);     // Progress
                DA.SetData(2, 0);     // Status
                DA.SetData(3, null);  // Content
                DA.SetData(4, null);  // Error
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

                if (!File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File does not exist");
                    return;
                }

                if (string.IsNullOrEmpty(fieldName))
                {
                    fieldName = "file";
                }

                // Use actual filename if not specified
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Path.GetFileName(path);
                }

                // Auto-detect content type if not specified
                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = MimeTypeUtility.GetMimeType(path);
                }

                var headerList = headers.Select(h => h.Value).ToList();
                var fieldList = fields.Select(f => f.Value).ToList();
                var token = CancelToken;
                long fileSize = new FileInfo(path).Length;

                this.Message = $"Starting... ({FormatBytes(fileSize)})";

                TaskList.Add(Task.Run(async () =>
                {
                    return await UploadMultipartAsync(url, path, fieldName, fileName, contentType, fieldList, headerList, fileSize, token);
                }, token));

                return;
            }

            // Post-solve: output results
            if (!GetSolveResults(DA, out UploadFileResult result))
            {
                DA.SetData(0, false);
                DA.SetData(1, 0);
                DA.SetData(2, 0);
                DA.SetData(3, null);
                DA.SetData(4, "No result");
                return;
            }

            if (result != null)
            {
                DA.SetData(0, result.Success);   // Done
                DA.SetData(1, result.Progress);  // Progress
                DA.SetData(2, result.StatusCode); // Status
                DA.SetData(3, result.Content);   // Content
                DA.SetData(4, result.Error);     // Error

                if (result.Success)
                {
                    this.Message = "Done";
                }
                else
                {
                    this.Message = "Failed";
                }
            }
        }

        private async Task<UploadFileResult> UploadMultipartAsync(string url, string path, string fieldName, string fileName,
            string contentType, List<DataModels.Implementations.MultipartField> fields,
            List<DataModels.Implementations.HttpHeader> headers, long fileSize, CancellationToken token)
        {
            var result = new UploadFileResult { Progress = 0 };

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    // Add headers
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    var multipartContent = new MultipartFormDataContent();

                    // Add the file with progress tracking
                    var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
                    var progressStream = new ProgressStream(fileStream, bytesRead =>
                    {
                        if (fileSize > 0)
                        {
                            result.Progress = (int)((bytesRead * 100) / fileSize);
                        }
                        UpdateProgressMessage(bytesRead, fileSize);
                    });

                    var streamContent = new StreamContent(progressStream, BufferSize);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    multipartContent.Add(streamContent, fieldName, fileName);

                    // Add additional form fields
                    foreach (var field in fields)
                    {
                        if (!field.IsText && field.Bytes != null)
                        {
                            // Binary field (small, already in memory)
                            var byteContent = new ByteArrayContent(field.Bytes);
                            if (!string.IsNullOrEmpty(field.ContentType))
                            {
                                byteContent.Headers.ContentType = new MediaTypeHeaderValue(field.ContentType);
                            }
                            multipartContent.Add(byteContent, field.Name ?? "file", field.FileName ?? "file");
                        }
                        else
                        {
                            // Text field
                            var stringContent = new StringContent(field.Text ?? string.Empty);
                            if (!string.IsNullOrEmpty(field.ContentType))
                            {
                                stringContent.Headers.ContentType = new MediaTypeHeaderValue(field.ContentType);
                            }
                            multipartContent.Add(stringContent, field.Name ?? "field");
                        }
                    }

                    request.Content = multipartContent;

                    using (var response = await HttpClientFactory.SharedClient.SendAsync(request, token))
                    {
                        result.StatusCode = (int)response.StatusCode;
                        result.Content = await response.Content.ReadAsStringAsync();
                        result.Success = response.IsSuccessStatusCode;
                        result.Progress = 100;

                        if (!result.Success)
                        {
                            result.Error = $"HTTP {result.StatusCode}: {response.ReasonPhrase}";
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Error = "Upload cancelled";
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.Error = $"HTTP error: {ex.Message}";
            }
            catch (IOException ex)
            {
                result.Success = false;
                result.Error = $"File error: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        private void UpdateProgressMessage(long bytesUploaded, long totalBytes)
        {
            var now = DateTime.Now;
            if (now - _lastProgressUpdate < _progressUpdateInterval)
                return;

            _lastProgressUpdate = now;

            string message;
            if (totalBytes > 0)
            {
                int percent = (int)((bytesUploaded * 100) / totalBytes);
                message = $"Uploading... {percent}% ({FormatBytes(bytesUploaded)} / {FormatBytes(totalBytes)})";
            }
            else
            {
                message = $"Uploading... {FormatBytes(bytesUploaded)}";
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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Icons_download_file; // TODO: Add icon

        public override Guid ComponentGuid => new Guid("F3A4B5C6-D7E8-9F00-1122-334455667788");
    }
}
