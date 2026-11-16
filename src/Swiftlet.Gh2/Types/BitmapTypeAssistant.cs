using Grasshopper2.Data;
using Grasshopper2.Types.Assistant;
using GrasshopperIO;
using Swiftlet.Imaging;

namespace Swiftlet.Gh2.Types;

public sealed class BitmapTypeAssistant : TypeAssistant<SwiftletImage>
{
    public BitmapTypeAssistant() : base("Bitmap") { }

    public override SwiftletImage Copy(SwiftletImage instance)
        => new(instance.Width, instance.Height, instance.GetPixelBytes());

    public override string DescribePrimary(Pear<SwiftletImage> pear)
        => $"BITMAP [ {pear.Item.Width} x {pear.Item.Height} ]";

    // Stored as one byte array: width and height (little-endian Int32) followed by RGBA pixels.
    public override bool Write(IWriter writer, Name location, SwiftletImage instance)
    {
        byte[] pixels = instance.GetPixelBytes();
        byte[] payload = new byte[8 + pixels.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), instance.Width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4, 4), instance.Height);

        pixels.CopyTo(payload, 8);
        writer.Integer8Array(location, payload);
        return true;
    }

    public override bool Read(IReader reader, Name location, out SwiftletImage instance)
    {
        instance = null!;
        if (!reader.HasItem(location))
            return false;

        byte[] payload = reader.Integer8Array(location);
        if (payload is null || payload.Length < 8)
            return false;

        int width = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4));
        int height = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4, 4));
        if (width <= 0 || height <= 0 || payload.Length - 8 != (long)width * height * 4)
            return false;

        instance = new SwiftletImage(width, height, payload.AsSpan(8).ToArray());
        return true;
    }
}
