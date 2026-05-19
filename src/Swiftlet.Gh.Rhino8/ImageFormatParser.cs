using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8;

internal static class ImageFormatParser
{
    public static bool TryParse(string? format, out SwiftletImageFormat imageFormat)
    {
        switch ((format ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "png":
                imageFormat = SwiftletImageFormat.Png;
                return true;
            case "bmp":
                imageFormat = SwiftletImageFormat.Bmp;
                return true;
            case "jpeg":
            case "jpg":
                imageFormat = SwiftletImageFormat.Jpeg;
                return true;
            case "gif":
                imageFormat = SwiftletImageFormat.Gif;
                return true;
            case "tiff":
            case "tif":
                imageFormat = SwiftletImageFormat.Tiff;
                return true;
            default:
                imageFormat = default;
                return false;
        }
    }
}
