using System.Drawing;
using System.Drawing.Imaging;
using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8;

internal static class SystemDrawingBitmapConverter
{
    public static bool TryToBitmap(SwiftletImage image, out Bitmap? bitmap)
    {
        try
        {
            bitmap = ToBitmap(image);
            return true;
        }
        catch (Exception ex) when (IsSystemDrawingUnavailable(ex))
        {
            bitmap = null;
            return false;
        }
    }

    public static bool TryToSwiftletImage(Bitmap bitmap, out SwiftletImage? image)
    {
        try
        {
            image = ToSwiftletImage(bitmap);
            return true;
        }
        catch (Exception ex) when (IsSystemDrawingUnavailable(ex))
        {
            image = null;
            return false;
        }
    }

    public static Bitmap ToBitmap(SwiftletImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                SwiftletColor pixel = image.GetPixel(x, y);
                bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
            }
        }

        return bitmap;
    }

    public static SwiftletImage ToSwiftletImage(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        int width = bitmap.Width;
        int height = bitmap.Height;
        var pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                int offset = ((y * width) + x) * 4;
                pixels[offset] = pixel.R;
                pixels[offset + 1] = pixel.G;
                pixels[offset + 2] = pixel.B;
                pixels[offset + 3] = pixel.A;
            }
        }

        return new SwiftletImage(width, height, pixels);
    }

    private static bool IsSystemDrawingUnavailable(Exception ex)
    {
        return ex is PlatformNotSupportedException
                   or DllNotFoundException
                   or FileNotFoundException
                   or FileLoadException
                   or TypeLoadException ||
               ex.InnerException is not null && IsSystemDrawingUnavailable(ex.InnerException);
    }
}
