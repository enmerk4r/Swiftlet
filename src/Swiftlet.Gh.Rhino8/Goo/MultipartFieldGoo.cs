using Grasshopper.Kernel.Types;
using Swiftlet.Core.Http;

namespace Swiftlet.Gh.Rhino8.Goo;

public sealed class MultipartFieldGoo : GH_Goo<MultipartField>
{
    public override bool IsValid => Value is not null;

    public override string TypeName => "Multipart Field";

    public override string TypeDescription => "A multipart/form-data field";

    public MultipartFieldGoo()
    {
        Value = new MultipartField(string.Empty, Array.Empty<byte>());
    }

    public MultipartFieldGoo(MultipartField? field)
    {
        Value = field?.Duplicate() ?? new MultipartField(string.Empty, Array.Empty<byte>());
    }

    public override IGH_Goo Duplicate() => new MultipartFieldGoo(Value);

    public override string ToString()
    {
        if (Value is null)
        {
            return "MULTIPART FIELD";
        }

        string name = string.IsNullOrWhiteSpace(Value?.Name) ? "<unnamed>" : Value.Name;
        int length = Value?.Bytes?.Length ?? 0;
        return $"MULTIPART FIELD [ {name} | {length} bytes ]";
    }
}
