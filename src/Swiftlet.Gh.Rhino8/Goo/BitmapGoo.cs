using Grasshopper.Kernel.Types;
using Swiftlet.Imaging;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class BitmapGoo : GH_Goo<SwiftletImage>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "Bitmap";

    public override string TypeDescription => "Grasshopper wrapper for a bitmap";

    public BitmapGoo()
    {
        Value = default!;
    }

    public BitmapGoo(SwiftletImage? bitmap)
    {
        Value = bitmap!;
    }

    public override IGH_Goo Duplicate()
    {
        return new BitmapGoo(Value is null
            ? null
            : new SwiftletImage(Value.Width, Value.Height, Value.GetPixelBytes()));
    }

    public override string ToString()
    {
        return Value is null ? "Null Bitmap" : $"BITMAP [ {Value.Width} x {Value.Height} ]";
    }

    public override bool CastTo<Q>(ref Q target)
    {
        if (Value is not null && typeof(Q).IsAssignableFrom(typeof(SwiftletImage)))
        {
            object bitmap = Value;
            target = (Q)bitmap;
            return true;
        }

        return base.CastTo(ref target);
    }

    public override bool CastFrom(object source)
    {
        switch (source)
        {
            case SwiftletImage bitmap:
                Value = bitmap;
                return true;
            case BitmapGoo goo:
                Value = goo.Value;
                return true;
            default:
                return base.CastFrom(source);
        }
    }
}
