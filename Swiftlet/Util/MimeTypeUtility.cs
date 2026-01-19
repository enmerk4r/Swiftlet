using System;
using System.Collections.Generic;
using System.IO;

namespace Swiftlet.Util
{
    /// <summary>
    /// Utility class for detecting MIME types from file extensions.
    /// </summary>
    public static class MimeTypeUtility
    {
        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".webp", "image/webp" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },
            { ".tif", "image/tiff" },
            { ".tiff", "image/tiff" },

            // Documents
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

            // Data formats
            { ".json", "application/json" },
            { ".xml", "application/xml" },
            { ".csv", "text/csv" },
            { ".txt", "text/plain" },
            { ".html", "text/html" },
            { ".htm", "text/html" },
            { ".css", "text/css" },
            { ".js", "application/javascript" },

            // Archives
            { ".zip", "application/zip" },
            { ".gz", "application/gzip" },
            { ".tar", "application/x-tar" },
            { ".rar", "application/vnd.rar" },
            { ".7z", "application/x-7z-compressed" },

            // Audio
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },
            { ".m4a", "audio/mp4" },

            // Video
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" },
            { ".mov", "video/quicktime" },
            { ".webm", "video/webm" },
            { ".mkv", "video/x-matroska" },

            // 3D/CAD (relevant for Rhino users)
            { ".3dm", "application/octet-stream" },
            { ".3ds", "application/octet-stream" },
            { ".obj", "model/obj" },
            { ".stl", "model/stl" },
            { ".step", "application/step" },
            { ".stp", "application/step" },
            { ".iges", "model/iges" },
            { ".igs", "model/iges" },
            { ".fbx", "application/octet-stream" },
            { ".dxf", "application/dxf" },
            { ".dwg", "application/acad" },
            { ".gltf", "model/gltf+json" },
            { ".glb", "model/gltf-binary" },
            { ".ply", "application/octet-stream" },
        };

        /// <summary>
        /// Gets the MIME type for a file based on its extension.
        /// </summary>
        /// <param name="filePath">File path or filename with extension</param>
        /// <returns>MIME type string, defaults to application/octet-stream if unknown</returns>
        public static string GetMimeType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "application/octet-stream";

            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
                return "application/octet-stream";

            return MimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
        }
    }
}
