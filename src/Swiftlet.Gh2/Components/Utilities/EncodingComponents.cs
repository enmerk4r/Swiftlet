using Grasshopper2.Components;
using Grasshopper2.Parameters;
using GrasshopperIO;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

// "7. Utilities": text, Base64, byte array, compression, CSV and GUID components.

internal static class UtilityInputs
{
    public const string EncodingInfo = "Can be ASCII, Unicode, UTF8, UTF7, UTF32";

    /// <summary>Text encoding input with the Grasshopper 1 default of UTF8.</summary>
    public static void AddEncoding(InputAdder inputs) => inputs.AddText("Encoding", "En", EncodingInfo).Set("UTF8");

    public static string Encoding(IDataAccess access, int index)
        => access.GetItem(index, out string encoding) && encoding is not null ? encoding : "UTF8";
}

[IoId(ComponentIds.Base64ToByteArray)]
public sealed class Base64ToByteArrayComponent : SwiftletComponent
{
    public Base64ToByteArrayComponent() : base("Base64 to Byte Array", "Converts a Base64 encoded string to a byte array", Sections.Utilities) { }
    public Base64ToByteArrayComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.AddText("Base64", "B6", "Base64 encoded string");

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Byte Array", "By", "Output Byte Array"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        // Like Grasshopper 1, a failure here faults the component (no output at all) rather than yielding a null.
        access.SetItem(0, Convert.FromBase64String(text ?? string.Empty));
    }
}

[IoId(ComponentIds.Base64ToText)]
public sealed class Base64ToTextComponent : SwiftletComponent
{
    public Base64ToTextComponent() : base("Base64 to Text", "Converts a Base64 encoded string to cleartext", Sections.Utilities) { }
    public Base64ToTextComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Base64", "B6", "Base64 encoded string");
        UtilityInputs.AddEncoding(inputs);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Text", "Tx", "Converted Text");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        string encoding = UtilityInputs.Encoding(access, 1);
        try
        {
            byte[] bytes = Convert.FromBase64String(text ?? string.Empty);
            access.SetItem(0, UtilityEncoding.Resolve(encoding).GetString(bytes));
        }
        catch (FormatException ex)
        {
            access.AddError("Invalid Base64", ex.Message);
        }
        catch (ArgumentException ex)
        {
            access.AddError("Unknown encoding", ex.Message);
        }
    }
}

[IoId(ComponentIds.ByteArrayToBase64)]
public sealed class ByteArrayToBase64Component : SwiftletComponent
{
    public ByteArrayToBase64Component() : base("Byte Array To Base64", "Converts a Byte Array to a Base64 encoded string", Sections.Utilities) { }
    public ByteArrayToBase64Component(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.Add(new ByteArrayParameter("Byte Array", "By", "Input Byte Array"));

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Base64", "B6", "Base64 encoded string");

    protected override void Process(IDataAccess access)
    {
        if (access.GetItem(0, out byte[] bytes) && bytes is not null)
            access.SetItem(0, Convert.ToBase64String(bytes));
    }
}

[IoId(ComponentIds.ByteArrayToList)]
public sealed class ByteArrayToListComponent : SwiftletComponent
{
    public ByteArrayToListComponent() : base("Byte Array To List", "Converts a byte array into a list of integers", Sections.Utilities) { }
    public ByteArrayToListComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.Add(new ByteArrayParameter("Byte Array", "By", "Byte Array to convert"));

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddInteger("Bytes", "Bt", "Byte Array as a list of integers", Access.Twig);

    protected override void Process(IDataAccess access)
    {
        if (access.GetItem(0, out byte[] bytes) && bytes is not null)
            access.SetTwig(0, bytes.Select(static value => (int)value).ToArray());
    }
}

[IoId(ComponentIds.ListToByteArray)]
public sealed class ListToByteArrayComponent : SwiftletComponent
{
    public ListToByteArrayComponent() : base("List To Byte Array", "Converts a list of integers into a byte array", Sections.Utilities) { }
    public ListToByteArrayComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.AddInteger("Bytes", "Bt", "Input as a list of integers", Access.Twig);

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Byte Array", "By", "Byte Array"));

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<int>(0, out var values);
        int[] items = values?.NonNullItems.ToArray() ?? Array.Empty<int>();
        access.SetItem(0, items.Select(static value => (byte)value).ToArray());
    }
}

[IoId(ComponentIds.ByteArrayToText)]
public sealed class ByteArrayToTextComponent : SwiftletComponent
{
    public ByteArrayToTextComponent() : base("Byte Array To Text", "Converts a byte array to text", Sections.Utilities) { }
    public ByteArrayToTextComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new ByteArrayParameter("Byte Array", "By", "Input Byte Array"));
        UtilityInputs.AddEncoding(inputs);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Text", "Tx", "Output text");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out byte[] bytes);
        string encoding = UtilityInputs.Encoding(access, 1);
        if (bytes is null)
            return;

        try
        {
            access.SetItem(0, UtilityEncoding.Resolve(encoding).GetString(bytes));
        }
        catch (ArgumentException ex)
        {
            access.AddError("Unknown encoding", ex.Message);
        }
    }
}

[IoId(ComponentIds.TextToBase64)]
public sealed class TextToBase64Component : SwiftletComponent
{
    public TextToBase64Component() : base("Text To Base64", "Converts text to a Base64 encoded string", Sections.Utilities) { }
    public TextToBase64Component(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Text", "Tx", "Input Text");
        UtilityInputs.AddEncoding(inputs);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Base64", "B6", "Base64 encoded string");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        string encoding = UtilityInputs.Encoding(access, 1);
        try
        {
            access.SetItem(0, Convert.ToBase64String(UtilityEncoding.Resolve(encoding).GetBytes(text ?? string.Empty)));
        }
        catch (ArgumentException ex)
        {
            access.AddError("Unknown encoding", ex.Message);
        }
    }
}

[IoId(ComponentIds.TextToByteArray)]
public sealed class TextToByteArrayComponent : SwiftletComponent
{
    public TextToByteArrayComponent() : base("Text To Byte Array", "Converts text to a byte array", Sections.Utilities) { }
    public TextToByteArrayComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Text", "Tx", "Input Text");
        UtilityInputs.AddEncoding(inputs);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Byte Array", "By", "Byte Array"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        string encoding = UtilityInputs.Encoding(access, 1);
        try
        {
            access.SetItem(0, UtilityEncoding.Resolve(encoding).GetBytes(text ?? string.Empty));
        }
        catch (ArgumentException ex)
        {
            access.AddError("Unknown encoding", ex.Message);
        }
    }
}

[IoId(ComponentIds.UrlEncode)]
public sealed class UrlEncodeComponent : SwiftletComponent
{
    public UrlEncodeComponent() : base("URL encode", "URL-encodes a string to make it URL-safe", Sections.Utilities) { }
    public UrlEncodeComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Text", "Tx", "Text to be URL encoded");
        UtilityInputs.AddEncoding(inputs);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Encoded", "Ec", "URL-encoded string");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        string encoding = UtilityInputs.Encoding(access, 1);
        try
        {
            access.SetItem(0, UtilityUrlEncoding.Encode(text ?? string.Empty, UtilityEncoding.Resolve(encoding)));
        }
        catch (ArgumentException ex)
        {
            access.AddError("Unknown encoding", ex.Message);
        }
    }
}

[IoId(ComponentIds.CompressData)]
public sealed class CompressDataComponent : SwiftletComponent
{
    public CompressDataComponent() : base("Compress Data", "GZIP byte array data", Sections.Utilities) { }
    public CompressDataComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.Add(new ByteArrayParameter("Uncompressed", "Un", "Uncompressed text"));

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Compressed", "Cm", "Compressed text"));

    protected override void Process(IDataAccess access)
    {
        if (access.GetItem(0, out byte[] input) && input is not null)
            access.SetItem(0, UtilityCompression.Compress(input));
    }
}

[IoId(ComponentIds.DecompressData)]
public sealed class DecompressDataComponent : SwiftletComponent
{
    public DecompressDataComponent() : base("Decompress Data", "Un-GZIP byte array data", Sections.Utilities) { }
    public DecompressDataComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.Add(new ByteArrayParameter("Compressed", "Cm", "Compressed text"));

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Uncompressed", "Un", "Uncompressed text"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out byte[] input) || input is null)
            return;

        // Like Grasshopper 1, a failure here faults the component (no output at all) rather than yielding a null.
        access.SetItem(0, UtilityCompression.Decompress(input));
    }
}

[IoId(ComponentIds.CreateCsvLine)]
public sealed class CreateCsvLineComponent : SwiftletComponent
{
    public CreateCsvLineComponent() : base("Create CSV Line", "Formats multiple strings as a single line of delimeter-separated values", Sections.Utilities) { }
    public CreateCsvLineComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Cells", "Cl", "Cell values of a CSV line", Access.Twig);
        inputs.AddText("Delimiter", "Dl", "CSV delimeter").Set(",");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Line", "Ln", "Formatted CSV line");

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<string>(0, out var cells);
        if (!access.GetItem(1, out string delimiter) || delimiter is null)
            delimiter = ",";

        access.SetItem(0, CsvLines.Create(cells?.NonNullItems ?? Array.Empty<string>(), delimiter));
    }
}

[IoId(ComponentIds.ReadCsvLine)]
public sealed class ReadCsvLineComponent : SwiftletComponent
{
    public ReadCsvLineComponent() : base("Read CSV Line", "Extracts individual values from a delimeter-separated line", Sections.Utilities) { }
    public ReadCsvLineComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Line", "Ln", "Formatted CSV line");
        inputs.AddText("Delimiter", "Dl", "CSV delimeter").Set(",");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Cells", "Cl", "Cell values of a CSV line", Access.Twig);

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string line);
        if (!access.GetItem(1, out string delimiter) || delimiter is null)
            delimiter = ",";

        try
        {
            access.SetTwig(0, CsvLines.Read(line ?? string.Empty, delimiter).ToArray());
        }
        catch (Microsoft.VisualBasic.FileIO.MalformedLineException ex)
        {
            access.AddError("Malformed CSV line", ex.Message);
        }
    }
}

[IoId(ComponentIds.SplitTextIntoLines)]
public sealed class SplitTextIntoLinesComponent : SwiftletComponent
{
    public SplitTextIntoLinesComponent() : base("Split Text Into Lines", "Takes a block of text and splits it into individual lines by the newline character", Sections.Utilities) { }
    public SplitTextIntoLinesComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.AddText("Text", "Tx", "Text to split into lines");

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Lines", "Ln", "Individual lines", Access.Twig);

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string text);
        access.SetTwig(0, (text ?? string.Empty).Split('\n'));
    }
}

[IoId(ComponentIds.GenerateGuid)]
public sealed class GenerateGuidComponent : SwiftletComponent
{
    private const string FormatInfo =
        "Format of the GUID as a single letter:\n" +
        "\nN - 32 digits:" +
        "\n00000000000000000000000000000000\n" +
        "\nD - 32 digits separated by hyphens:" +
        "\n00000000-0000-0000-0000-000000000000\n" +
        "\nB - 32 digits separated by hyphens, enclosed in braces:" +
        "\n{00000000-0000-0000-0000-000000000000}\n" +
        "\nP - 32 digits separated by hyphens, enclosed in parentheses:" +
        "\n(00000000-0000-0000-0000-000000000000)\n" +
        "\nX - Four hexadecimal values enclosed in braces, where the fourth value is a subset of eight hexadecimal values that is also enclosed in braces:" +
        "\n{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}\n";

    public GenerateGuidComponent() : base("Generate GUID", "Generates a random GUID (Globally Unique Identifier)\nThis is useful if you need a quick way to reliably generate unique IDs", Sections.Utilities) { }
    public GenerateGuidComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddBoolean("Generate", "Gn", "Generate a GUID!").Set(true);
        inputs.AddText("Format", "Fm", FormatInfo).Set("D");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("GUID", "Gd", "Random GUID");

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out bool generate))
            generate = true;
        if (!access.GetItem(1, out string format) || format is null)
            format = "D";

        if (!generate)
            return;

        try
        {
            access.SetItem(0, Guid.NewGuid().ToString(format));
        }
        catch (FormatException ex)
        {
            access.AddError("Invalid GUID format", ex.Message);
        }
    }
}
