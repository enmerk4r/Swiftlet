using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace Swiftlet.Imaging;

public static class ImageCodec
{
    public static SwiftletImage Load(byte[] encodedBytes)
    {
        if (encodedBytes is null || encodedBytes.Length == 0)
        {
            throw new ArgumentException("Image bytes are required.", nameof(encodedBytes));
        }

        using Image<Rgba32> image = Image.Load<Rgba32>(encodedBytes);
        var pixelData = new Rgba32[image.Width * image.Height];
        image.CopyPixelDataTo(pixelData);

        var bytes = new byte[pixelData.Length * 4];
        for (int index = 0; index < pixelData.Length; index++)
        {
            int offset = index * 4;
            bytes[offset] = pixelData[index].R;
            bytes[offset + 1] = pixelData[index].G;
            bytes[offset + 2] = pixelData[index].B;
            bytes[offset + 3] = pixelData[index].A;
        }

        return new SwiftletImage(image.Width, image.Height, bytes);
    }

    public static byte[] Save(SwiftletImage image, SwiftletImageFormat format)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        using var output = new MemoryStream();
        using Image<Rgba32> encoded = ToImageSharp(image);

        switch (format)
        {
            case SwiftletImageFormat.Png:
                encoded.Save(output, new PngEncoder());
                break;
            case SwiftletImageFormat.Bmp:
                encoded.Save(output, new BmpEncoder());
                break;
            case SwiftletImageFormat.Jpeg:
                encoded.Save(output, new JpegEncoder());
                break;
            case SwiftletImageFormat.Gif:
                encoded.Save(output, new GifEncoder());
                break;
            case SwiftletImageFormat.Tiff:
                encoded.Save(output, new TiffEncoder());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }

        return output.ToArray();
    }

    public static SwiftletImage GenerateQrCode(string text, int pixelsPerModule, SwiftletColor darkColor, SwiftletColor lightColor, bool drawQuietZones)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("QR code text is required.", nameof(text));
        }

        if (pixelsPerModule <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerModule));
        }

        using var generator = new QRCodeGenerator();
        using QRCodeData data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        int quietZoneSize = drawQuietZones ? 0 : 4;
        int moduleCount = data.ModuleMatrix.Count - (quietZoneSize * 2);
        int width = moduleCount * pixelsPerModule;
        int height = moduleCount * pixelsPerModule;
        var pixels = new byte[width * height * 4];
        var image = new SwiftletImage(width, height, pixels);

        for (int moduleY = 0; moduleY < moduleCount; moduleY++)
        {
            for (int moduleX = 0; moduleX < moduleCount; moduleX++)
            {
                bool isDark = data.ModuleMatrix[moduleY + quietZoneSize][moduleX + quietZoneSize];
                SwiftletColor color = isDark ? darkColor : lightColor;
                PaintModule(image, moduleX, moduleY, pixelsPerModule, color);
            }
        }

        return image;
    }

    private static Image<Rgba32> ToImageSharp(SwiftletImage image)
    {
        var encoded = new Image<Rgba32>(image.Width, image.Height);

        encoded.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    SwiftletColor pixel = image.GetPixel(x, y);
                    row[x] = new Rgba32(pixel.R, pixel.G, pixel.B, pixel.A);
                }
            }
        });

        return encoded;
    }

    private static void PaintModule(SwiftletImage image, int moduleX, int moduleY, int pixelsPerModule, SwiftletColor color)
    {
        int xStart = moduleX * pixelsPerModule;
        int yStart = moduleY * pixelsPerModule;

        for (int y = yStart; y < yStart + pixelsPerModule; y++)
        {
            for (int x = xStart; x < xStart + pixelsPerModule; x++)
            {
                image.SetPixel(x, y, color);
            }
        }
    }
}
