using System.Reflection;
using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Components;

namespace Swiftlet.Gh2.Migration;

// Grasshopper 1 -> Grasshopper 2 rules for "7. Utilities".
public sealed class UtilitiesMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("792af514-90fe-45b0-82d1-2bb90c55e813", ComponentIds.Base64ToByteArray, 1, 1); // Base64 to Byte Array
        yield return Gh1MigrationRule.Identity("2385c560-3f23-4196-bb17-5a0c8f7b4aca", ComponentIds.Base64ToText, 2, 1); // Base64 to Text
        yield return Gh1MigrationRule.Identity("3511b9b7-db9f-409c-8cf8-2a67e5bc952d", ComponentIds.BitmapToByteArray, 2, 1); // Bitmap to Byte Array
        yield return Gh1MigrationRule.Identity("dae3641e-a624-4e2f-9aad-b278301f84a1", ComponentIds.BitmapToMesh, 2, 4); // Bitmap to Mesh
        yield return Gh1MigrationRule.Identity("143261e1-c3d1-45b3-b67e-c3a510eb753e", ComponentIds.ByteArrayToBase64, 1, 1); // Byte Array To Base64
        yield return Gh1MigrationRule.Identity("7e29f95c-56ae-4d28-9290-a51dc4d956f1", ComponentIds.ByteArrayToBitmap, 1, 1); // Byte Array To Bitmap
        yield return Gh1MigrationRule.Identity("60e16712-a6aa-4e02-860a-d587b512320c", ComponentIds.ByteArrayToList, 1, 1); // Byte Array To List
        yield return Gh1MigrationRule.Identity("15e4454a-ee5b-483f-886f-f10307421ff7", ComponentIds.ByteArrayToText, 2, 1); // Byte Array To Text
        yield return Gh1MigrationRule.Identity("921383a8-97e2-434d-bd02-560312ad4fd8", ComponentIds.ByteArrayToFile, 2, 1); // Byte Array to File
        yield return Gh1MigrationRule.Identity("2e6f37c8-5c0c-4e70-9084-dcb7705871f2", ComponentIds.ColorToHex, 1, 1); // Color to Hex
        yield return Gh1MigrationRule.Identity("9a8755b2-86ff-432b-9a9e-dd8639dd4dfb", ComponentIds.CompressData, 1, 1); // Compress Data
        yield return Gh1MigrationRule.Identity("d9b2041e-ff4e-446e-b634-4ebacd47051e", ComponentIds.CreateCsvLine, 2, 1); // Create CSV Line
        yield return Gh1MigrationRule.Identity("4da74eea-0134-4ab4-8e68-7e929e839bbe", ComponentIds.DecompressData, 1, 1); // Decompress Data
        yield return Gh1MigrationRule.Identity("6514b6d2-779e-4cd6-92b7-91a2f4a36360", ComponentIds.FileToByteArray, 1, 1); // File To Byte Array
        yield return Gh1MigrationRule.Identity("e4cd167a-1d5b-4c43-a1db-70da520dbde7", ComponentIds.GenerateGuid, 2, 1); // Generate GUID
        yield return Gh1MigrationRule.Identity("fe44460c-871a-4098-a948-48aa02977410", ComponentIds.GenerateQrCode, 5, 1); // Generate QR code
        yield return Gh1MigrationRule.Identity("980e2b6e-3f59-4d1c-831b-77446e2dc407", ComponentIds.HexToColor, 1, 1); // Hex to Color
        yield return Gh1MigrationRule.Identity("bdfd03f3-42cb-48db-9f5a-607fcf3143e2", ComponentIds.ListToByteArray, 1, 1); // List To Byte Array
        yield return Gh1MigrationRule.Identity("0478d672-bfbd-4502-b627-07b2e5d7f664", ComponentIds.ReadCsvLine, 2, 1); // Read CSV Line
        yield return Gh1MigrationRule.Identity("1b49e53c-43ae-4a66-8615-c7db063465da", ComponentIds.ReplaceEmptyBranches, 2, 1); // Replace Empty Branches
        yield return Gh1MigrationRule.Identity("c8f5887a-30f1-4fe5-a737-dae37bdacc5c", ComponentIds.SaveCsv, 2, 1); // Save CSV
        yield return Gh1MigrationRule.Identity("1010fb58-d4a6-462d-810e-d5ee1b920ff4", ComponentIds.SaveText, 2, 1); // Save Text
        yield return Gh1MigrationRule.Identity("3561ef51-707a-414b-b344-68b79108bc46", ComponentIds.SplitTextIntoLines, 1, 1); // Split Text Into Lines
        yield return Gh1MigrationRule.Identity("ac216550-bea2-4f2d-b9fa-c5b0df90c423", ComponentIds.TextToBase64, 2, 1); // Text To Base64
        yield return Gh1MigrationRule.Identity("52c16df2-35b5-4315-bb02-4d0c2aa1dacf", ComponentIds.TextToByteArray, 2, 1); // Text To Byte Array
        yield return Gh1MigrationRule.Identity("9d385c97-7975-43a3-9ae7-2fabbe5bd4c8", ComponentIds.UrlEncode, 2, 1); // URL encode
        yield return Gh1MigrationRule.Identity("de7a191a-b5ce-4eee-a734-9d15f4c44970", ComponentIds.ScreenCaptureActiveViewport, 1, 1); // Screen Capture Active Viewport
        yield return Gh1MigrationRule.Identity("808b6c0c-e4d7-4451-984a-d4a77c4c83eb", ComponentIds.ScreenCaptureViewport, 2, 1); // Screen Capture Viewport
    }
}

/// <summary>Save Web Response carries its Text/Binary choice, which Grasshopper 1 saved with the component.</summary>
public sealed class SaveWebResponseMigration : IMigrateComponent
{
    public Guid Grasshopper1Id => new("e978195c-b6dc-4be5-bf98-0246bbacec7c");

    public IDocumentObject Migrate(IGH_Component source)
    {
        var rule = Gh1MigrationRule.Identity(Grasshopper1Id.ToString(), ComponentIds.SaveWebResponse, 2, 1);
        var target = (SaveWebResponseComponent)((IMigrateComponent)rule).Migrate(source);

        // GH1 Read has already restored the menu state; read it without referencing Grasshopper.dll.
        FieldInfo? textChecked = source.UnderlyingObject.GetType().GetField("_textChecked", BindingFlags.Instance | BindingFlags.NonPublic);
        if (textChecked?.GetValue(source.UnderlyingObject) is not bool saveAsText)
            throw new InvalidOperationException("Cannot migrate the saved Text/Binary setting of Save Web Response.");

        target.SaveAsText = saveAsText;
        return target;
    }
}
