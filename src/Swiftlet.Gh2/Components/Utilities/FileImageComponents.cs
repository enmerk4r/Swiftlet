using System.Drawing;
using Eto.Forms;
using Grasshopper2.Components;
using Grasshopper2.Data;
using Grasshopper2.Parameters;
using Grasshopper2.Types.Colour;
using Grasshopper2.UI.InputPanel;
using GrasshopperIO;
using Rhino.Geometry;
using Swiftlet.Core.Http;
using Swiftlet.Gh2.Parameters;
using Swiftlet.Imaging;
using Swiftlet.Runtime;

namespace Swiftlet.Gh2.Components;

// "7. Utilities": files, bitmaps, colours, QR codes and tree padding.

internal static class ColourBytes
{
    /// <summary>GH2 colours are floating point; Swiftlet works in 8-bit channels like Grasshopper 1.</summary>
    public static SwiftletColor ToSwiftlet(Colour colour)
    {
        Eto.Drawing.Color eto = colour.ToEto();
        return new SwiftletColor((byte)eto.Rb, (byte)eto.Gb, (byte)eto.Bb, (byte)eto.Ab);
    }

    public static Colour FromArgb(byte a, byte r, byte g, byte b) => Colour.FromGdi(Color.FromArgb(a, r, g, b));
}

[IoId(ComponentIds.ByteArrayToFile)]
public sealed class ByteArrayToFileComponent : SwiftletComponent
{
    public ByteArrayToFileComponent() : base("Byte Array to File", "Save a byte array to a local file", Sections.Utilities) { }
    public ByteArrayToFileComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new ByteArrayParameter("Byte Array", "By", "Byte Array to save"));
        inputs.AddText("Path", "Pt", "Output filepath");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddInteger("Bytes", "Bt", "Saved file size in bytes");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out byte[] bytes);
        access.GetItem(1, out string path);
        if (bytes is null)
            return;

        try
        {
            access.SetItem(0, checked((int)FileWriteUtility.WriteBytes(path ?? string.Empty, bytes)));
        }
        catch (Exception ex)
        {
            access.AddError("Failed to save file", ex.Message);
        }
    }
}

[IoId(ComponentIds.FileToByteArray)]
public sealed class FileToByteArrayComponent : SwiftletComponent
{
    public FileToByteArrayComponent() : base("File To Byte Array", "Read a file into a byte array", Sections.Utilities) { }
    public FileToByteArrayComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.AddText("Path", "Pt", "Input File");

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Byte Array", "By", "Byte Array"));

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string path);
        try
        {
            access.SetItem(0, File.ReadAllBytes(FilePathUtility.NormalizePath(path ?? string.Empty)));
        }
        catch (Exception ex)
        {
            access.AddError("Failed to read file", ex.Message);
        }
    }
}

[IoId(ComponentIds.SaveCsv)]
public sealed class SaveCsvComponent : SwiftletComponent
{
    public SaveCsvComponent() : base("Save CSV", "Save formatted CSV Lines as a CSV file", Sections.Utilities) { }
    public SaveCsvComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Lines", "Ln", "CSV-formatted lines (use the \"Create CSV Line\" component)", Access.Twig);
        inputs.AddText("Path", "Pt", "Output path");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddInteger("Bytes", "Bt", "Size of saved file in bytes");

    protected override void Process(IDataAccess access)
    {
        access.GetTwig<string>(0, out var lines);
        access.GetItem(1, out string path);
        try
        {
            long length = FileWriteUtility.WriteLines(path ?? string.Empty, lines?.NonNullItems ?? Array.Empty<string>());
            access.SetItem(0, checked((int)length));
        }
        catch (Exception ex)
        {
            access.AddError("Failed to save file", ex.Message);
        }
    }
}

[IoId(ComponentIds.SaveText)]
public sealed class SaveTextComponent : SwiftletComponent
{
    public SaveTextComponent() : base("Save Text", "Save text to disk", Sections.Utilities) { }
    public SaveTextComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Content", "Ct", "Text to be saved to disk");
        inputs.AddText("Path", "Pt", "Path to file");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddInteger("Bytes", "Bt", "Size of saved file in bytes");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string content);
        access.GetItem(1, out string path);
        try
        {
            access.SetItem(0, checked((int)FileWriteUtility.WriteText(path ?? string.Empty, content ?? string.Empty)));
        }
        catch (Exception ex)
        {
            access.AddError("Failed to save file", ex.Message);
        }
    }
}

/// <summary>Saves a web response either as its raw bytes (default) or as its text content.</summary>
[IoId(ComponentIds.SaveWebResponse)]
public sealed class SaveWebResponseComponent : SwiftletComponent
{
    private const string SaveAsTextKey = "SaveAsText";

    public SaveWebResponseComponent() : base("Save Web Response", "Save Web Response to disk", Sections.Utilities) => ShowMode();

    public SaveWebResponseComponent(IReader reader) : base(reader)
    {
        if (reader.HasItem(SaveAsTextKey))
            SaveAsText = reader.Boolean(SaveAsTextKey);
        ShowMode();
    }

    /// <summary>GH1 shows the save mode under the component at all times, not only after a solve.</summary>
    private void ShowMode() => ShowMessage(SaveAsText ? "Text" : "Binary");

    /// <summary>False saves the response bytes ("Binary" in Grasshopper 1); true saves the text content.</summary>
    public bool SaveAsText
    {
        get => CustomValues.Get(SaveAsTextKey, false);
        set => CustomValues.Set(SaveAsTextKey, value);
    }

    public override void Store(IWriter writer)
    {
        base.Store(writer);
        writer.Boolean(SaveAsTextKey, SaveAsText);
    }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new HttpResponseDataParameter("Response", "Rs", "Full Http response object (with metadata)"));
        inputs.AddText("Path", "Pt", "Path to file");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddInteger("Bytes", "Bt", "Size of saved file in bytes");

    protected override void Process(IDataAccess access)
    {
        ShowMode();
        access.GetItem(0, out HttpResponseData response);
        access.GetItem(1, out string path);
        if (response is null)
            return;

        try
        {
            long length = SaveAsText
                ? FileWriteUtility.WriteText(path ?? string.Empty, response.Content)
                : FileWriteUtility.WriteBytes(path ?? string.Empty, response.Bytes);
            access.SetItem(0, checked((int)length));
        }
        catch (Exception ex)
        {
            access.AddError("Failed to save file", ex.Message);
        }
    }

    public override void AppendToInputPanel(InputPanel panel)
    {
        base.AppendToInputPanel(panel);
        panel.AddLabel("Save as", false, "Save the response bytes as they are, or its text content");
        var picker = new DropDown();
        picker.Items.Add("Binary");
        picker.Items.Add("Text");
        picker.SelectedIndex = SaveAsText ? 1 : 0;
        picker.SelectedIndexChanged += (_, _) =>
        {
            if (picker.SelectedIndex < 0)
                return;

            AddUndoRecord(new Grasshopper2.Undo.VerbNoun("Change", "Save mode"),
                new Grasshopper2.Undo.Actions.CustomValueAction(this, SaveAsTextKey));
            SaveAsText = picker.SelectedIndex == 1;
            ShowMode();
            Expire();
            Document?.Solution.Start();
        };
        panel.Add(picker);
    }
}

[IoId(ComponentIds.BitmapToByteArray)]
public sealed class BitmapToByteArrayComponent : SwiftletComponent
{
    public BitmapToByteArrayComponent() : base("Bitmap to Byte Array", "Converts a bitmap to a byte array", Sections.Utilities) { }
    public BitmapToByteArrayComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new BitmapParameter("Bitmap", "Bm", "Input Bitmap"));
        inputs.AddText("Format", "Fm", "Image format (BMP, JPEG, PNG, GIF, TIFF, EMF, WMF, EXIF, ICON)");
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new ByteArrayParameter("Byte Array", "By", "Output Byte Array"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out SwiftletImage image) || image is null)
            return;

        access.GetItem(1, out string format);
        // Like Grasshopper 1, a failure here faults the component (no output at all) rather than yielding a null.
        if (!ImageFormatParser.TryParse(format, out SwiftletImageFormat imageFormat))
            throw new Exception($"Format {format} is not supported");

        access.SetItem(0, ImageCodec.Save(image, imageFormat));
    }
}

[IoId(ComponentIds.ByteArrayToBitmap)]
public sealed class ByteArrayToBitmapComponent : SwiftletComponent
{
    public ByteArrayToBitmapComponent() : base("Byte Array To Bitmap", "Converts a byte array to a bitmap", Sections.Utilities) { }
    public ByteArrayToBitmapComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.Add(new ByteArrayParameter("Byte Array", "By", "Input Byte Array"));

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new BitmapParameter("Bitmap", "Bm", "Output bitmap"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out byte[] bytes) || bytes is null)
            return;

        // Like Grasshopper 1, a failure here faults the component (no output at all) rather than yielding a null.
        access.SetItem(0, ImageCodec.Load(bytes));
    }
}

[IoId(ComponentIds.BitmapToMesh)]
public sealed class BitmapToMeshComponent : SwiftletComponent
{
    public BitmapToMeshComponent() : base("Bitmap to Mesh", "Converts a bitmap to a Rhino mesh", Sections.Utilities) { }
    public BitmapToMeshComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.Add(new BitmapParameter("Bitmap", "Bm", "Input Bitmap"));
        inputs.AddRectangle("Rectangle", "Rc", "Optional boundary", Access.Item, Requirement.MayBeMissing);
    }

    protected override void AddOutputs(OutputAdder outputs)
    {
        outputs.AddMesh("Mesh", "Ms", "Output colored mesh");
        outputs.AddColour("Colors", "Cl", "Bitmap colors (in the original order)", Access.Twig);
        outputs.AddInteger("Width", "Wd", "Bitmap width (in pixels)");
        outputs.AddInteger("Height", "Ht", "Bitmap height (in pixels)");
    }

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out SwiftletImage image) || image is null)
            return;

        Mesh mesh = access.GetItem(1, out Rectangle3d rect) && rect.IsValid
            ? Mesh.CreateFromPlane(rect.Plane, rect.X, rect.Y, image.Width - 1, image.Height - 1)
            : Mesh.CreateFromPlane(Plane.WorldXY, new Interval(0, image.Width), new Interval(0, image.Height), image.Width - 1, image.Height - 1);

        // Mesh vertices run bottom row first; the colour list is in the bitmap's own order (top row first).
        var colours = new Colour[image.Width * image.Height];
        for (int y = image.Height - 1; y >= 0; y--)
        {
            access.Solution.Token.ThrowIfCancellationRequested();
            for (int x = 0; x < image.Width; x++)
            {
                SwiftletColor pixel = image.GetPixel(x, y);
                mesh.VertexColors.Add(Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
                colours[(y * image.Width) + x] = ColourBytes.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
            }
        }

        access.SetItem(0, mesh);
        access.SetTwig(1, colours);
        access.SetItem(2, image.Width);
        access.SetItem(3, image.Height);
    }
}

[IoId(ComponentIds.GenerateQrCode)]
public sealed class GenerateQrCodeComponent : SwiftletComponent
{
    public GenerateQrCodeComponent() : base("Generate QR code", "Generates a QR code from a string", Sections.Utilities) { }
    public GenerateQrCodeComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddText("Text", "Tx", "Text to encode as a QR code (e.g. a URL)");
        inputs.AddInteger("Pixels", "Px", "Pixels per module").Set(20);
        inputs.AddColour("Dark", "Dk", "Dark color").Set(Colour.FromGdi(Color.Black));
        inputs.AddColour("Light", "Lt", "Light color").Set(Colour.FromGdi(Color.White));
        inputs.AddBoolean("Quiet", "Qt", "Draw quiet zones").Set(true);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.Add(new BitmapParameter("Bitmap", "Bm", "Output bitmap"));

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out string text) || text is null)
            return;
        if (!access.GetItem(1, out int pixels))
            pixels = 20;
        if (!access.GetItem(2, out Colour dark))
            dark = Colour.FromGdi(Color.Black);
        if (!access.GetItem(3, out Colour light))
            light = Colour.FromGdi(Color.White);
        if (!access.GetItem(4, out bool quiet))
            quiet = true;

        try
        {
            access.SetItem(0, ImageCodec.GenerateQrCode(text, pixels, ColourBytes.ToSwiftlet(dark), ColourBytes.ToSwiftlet(light), quiet));
        }
        catch (Exception ex)
        {
            access.AddError("Failed to generate QR code", ex.Message);
        }
    }
}

[IoId(ComponentIds.ColorToHex)]
public sealed class ColorToHexComponent : SwiftletComponent
{
    public ColorToHexComponent() : base("Color to Hex", "Converts a Color value to a hex code", Sections.Utilities) { }
    public ColorToHexComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.AddColour("Color", "Cl", "Color to convert");

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddText("Hex", "Hx", "Hex code");

    protected override void Process(IDataAccess access)
    {
        if (!access.GetItem(0, out Colour colour))
            return;

        SwiftletColor rgb = ColourBytes.ToSwiftlet(colour);
        access.SetItem(0, $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}");
    }
}

[IoId(ComponentIds.HexToColor)]
public sealed class HexToColorComponent : SwiftletComponent
{
    public HexToColorComponent() : base("Hex to Color", "Parses a hex code into a color value", Sections.Utilities) { }
    public HexToColorComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs) => inputs.AddText("Hex", "Hx", "Hex color code");

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddColour("Color", "Cl", "Color represented by the hex code");

    protected override void Process(IDataAccess access)
    {
        access.GetItem(0, out string hex);
        if (string.IsNullOrWhiteSpace(hex))
            return;

        try
        {
            string normalized = hex.StartsWith('#') ? hex : $"#{hex}";
            access.SetItem(0, Colour.FromGdi(ColorTranslator.FromHtml(normalized)));
        }
        catch
        {
            access.AddWarning("Invalid hex code", $"Unable to parse the hex code: {hex}");
        }
    }
}

/// <summary>Fills every empty branch of a tree with the replacement values.</summary>
[IoId(ComponentIds.ReplaceEmptyBranches)]
public sealed class ReplaceEmptyBranchesComponent : SwiftletComponent
{
    public ReplaceEmptyBranchesComponent() : base("Replace Empty Branches", "Substitutes all empty branches in a tree with a provided list of values.\nUseful for padding missing values in a web scraping scenario", Sections.Utilities) { }
    public ReplaceEmptyBranchesComponent(IReader reader) : base(reader) { }

    protected override void AddInputs(InputAdder inputs)
    {
        inputs.AddGeneric("Tree", "Tr", "Input tree", Access.Tree);
        inputs.AddGeneric("Replacement", "Rp", "List of values to replace empty branches with", Access.Twig);
    }

    protected override void AddOutputs(OutputAdder outputs) => outputs.AddGeneric("Tree", "Tr", "Output tree with padded empty branches", Access.Tree);

    protected override void Process(IDataAccess access)
    {
        if (!access.GetITree(0, out ITree tree) || tree is null)
            return;

        access.GetITwig(1, out ITwig replacement);
        replacement ??= Garden.ITwigEmpty();

        var twigs = new List<ITwig>(tree.PathCount);
        for (int index = 0; index < tree.PathCount; index++)
        {
            ITwig twig = tree.Twigs[index];
            twigs.Add(twig.LeafCount > 0 ? twig : replacement);
        }

        access.SetTree(0, Garden.ITreeFromITwigs(tree.Paths, twigs));
    }
}
