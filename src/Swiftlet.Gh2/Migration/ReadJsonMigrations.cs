using Grasshopper2.Doc.Migration;

namespace Swiftlet.Gh2.Migration;

/// <summary>GH1 -> GH2 migrations for the "4. Read JSON" tab.</summary>
public sealed class ReadJsonMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("37c912a2-2ab5-4926-8911-cb220b6adb25", ComponentIds.FormatJsonString, 1, 1);
    }
}
