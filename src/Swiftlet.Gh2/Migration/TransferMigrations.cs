using Grasshopper2.Doc.Migration;

namespace Swiftlet.Gh2.Migration;

/// <summary>
/// GH1 -> GH2 rules for Download File, Upload File and Upload File Multipart. They have no saved state
/// (the transfer is transient), so identity pin maps suffice. The "3. Send" migration table forwards to
/// <see cref="Create"/> so the tab has a single table.
/// </summary>
internal static class TransferMigrationRules
{
    public static IEnumerable<IMigrateBase> Create()
    {
        yield return Gh1MigrationRule.Identity("d1e2f3a4-b5c6-7d8e-9f00-112233445566", ComponentIds.DownloadFile, 6, 6);
        yield return Gh1MigrationRule.Identity("e2f3a4b5-c6d7-8e9f-0011-223344556677", ComponentIds.UploadFile, 7, 5);
        yield return Gh1MigrationRule.Identity("f3a4b5c6-d7e8-9f00-1122-334455667788", ComponentIds.UploadFileMultipart, 8, 5);
    }
}
