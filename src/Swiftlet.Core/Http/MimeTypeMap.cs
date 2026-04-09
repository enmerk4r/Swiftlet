using System.Collections.ObjectModel;

namespace Swiftlet.Core.Http;

public static class MimeTypeMap
{
    private static readonly IReadOnlyDictionary<string, string> MimeTypes =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml",
            [".ico"] = "image/x-icon",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".json"] = "application/json",
            [".xml"] = "application/xml",
            [".csv"] = "text/csv",
            [".txt"] = "text/plain",
            [".html"] = "text/html",
            [".htm"] = "text/html",
            [".css"] = "text/css",
            [".js"] = "application/javascript",
            [".zip"] = "application/zip",
            [".gz"] = "application/gzip",
            [".tar"] = "application/x-tar",
            [".rar"] = "application/vnd.rar",
            [".7z"] = "application/x-7z-compressed",
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".ogg"] = "audio/ogg",
            [".m4a"] = "audio/mp4",
            [".mp4"] = "video/mp4",
            [".avi"] = "video/x-msvideo",
            [".mov"] = "video/quicktime",
            [".webm"] = "video/webm",
            [".mkv"] = "video/x-matroska",
            [".3dm"] = ContentTypes.ApplicationOctetStream,
            [".3ds"] = ContentTypes.ApplicationOctetStream,
            [".obj"] = "model/obj",
            [".stl"] = "model/stl",
            [".step"] = "application/step",
            [".stp"] = "application/step",
            [".iges"] = "model/iges",
            [".igs"] = "model/iges",
            [".fbx"] = ContentTypes.ApplicationOctetStream,
            [".dxf"] = "application/dxf",
            [".dwg"] = "application/acad",
            [".gltf"] = "model/gltf+json",
            [".glb"] = "model/gltf-binary",
            [".ply"] = ContentTypes.ApplicationOctetStream,
        });

    public static string GetMimeType(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ContentTypes.ApplicationOctetStream;
        }

        string extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ContentTypes.ApplicationOctetStream;
        }

        return MimeTypes.TryGetValue(extension, out string? mimeType)
            ? mimeType
            : ContentTypes.ApplicationOctetStream;
    }
}
