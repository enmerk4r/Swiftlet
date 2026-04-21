namespace Swiftlet.Imaging;

public sealed class SwiftletImage
{
    private readonly byte[] _pixels;

    public SwiftletImage(int width, int height, byte[] pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (pixels is null)
        {
            throw new ArgumentNullException(nameof(pixels));
        }

        if (pixels.Length != width * height * 4)
        {
            throw new ArgumentException("Pixel buffer length does not match width and height.", nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels.ToArray();
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] GetPixelBytes()
    {
        return _pixels.ToArray();
    }

    public SwiftletColor GetPixel(int x, int y)
    {
        ValidateCoordinates(x, y);
        int offset = GetOffset(x, y);
        return new SwiftletColor(_pixels[offset], _pixels[offset + 1], _pixels[offset + 2], _pixels[offset + 3]);
    }

    public void SetPixel(int x, int y, SwiftletColor color)
    {
        ValidateCoordinates(x, y);
        int offset = GetOffset(x, y);
        _pixels[offset] = color.R;
        _pixels[offset + 1] = color.G;
        _pixels[offset + 2] = color.B;
        _pixels[offset + 3] = color.A;
    }

    private int GetOffset(int x, int y)
    {
        return ((y * Width) + x) * 4;
    }

    private void ValidateCoordinates(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }
    }
}
