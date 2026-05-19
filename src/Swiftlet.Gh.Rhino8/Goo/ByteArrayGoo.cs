using Grasshopper.Kernel.Types;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class ByteArrayGoo : GH_Goo<byte[]>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "Byte Array";

    public override string TypeDescription => "Grasshopper wrapper for a byte array";

    public ByteArrayGoo()
    {
        Value = Array.Empty<byte>();
    }

    public ByteArrayGoo(byte[]? data)
    {
        Value = data?.ToArray() ?? Array.Empty<byte>();
    }

    public override IGH_Goo Duplicate() => new ByteArrayGoo(Value);

    public override string ToString() => $"BYTE ARRAY [ {Value?.Length ?? 0} ]";
}
