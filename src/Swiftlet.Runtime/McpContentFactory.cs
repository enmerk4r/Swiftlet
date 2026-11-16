using Swiftlet.Core.Mcp;
using Swiftlet.Imaging;

namespace Swiftlet.Runtime;

public static class McpContentFactory
{
    public static McpImageContentBlock CreateImage(SwiftletImage image, string format)
    {
        if (!ImageFormatParser.TryParse(format, out SwiftletImageFormat parsed))
            throw new Exception($"Format {format} is not supported");
        string mime = parsed switch
        {
            SwiftletImageFormat.Png => "image/png",
            SwiftletImageFormat.Bmp => "image/bmp",
            SwiftletImageFormat.Jpeg => "image/jpeg",
            SwiftletImageFormat.Gif => "image/gif",
            SwiftletImageFormat.Tiff => "image/tiff",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        return new McpImageContentBlock(mime, Convert.ToBase64String(ImageCodec.Save(image, parsed)));
    }
}
