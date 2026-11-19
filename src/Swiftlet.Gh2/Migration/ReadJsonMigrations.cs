using Grasshopper2.Doc.Migration;

namespace Swiftlet.Gh2.Migration;

/// <summary>GH1 -> GH2 migrations for the "4. Read JSON" tab.</summary>
public sealed class ReadJsonMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("37c912a2-2ab5-4926-8911-cb220b6adb25", ComponentIds.FormatJsonString, 1, 1);
        yield return Gh1MigrationRule.Identity("18b4fd6e-e6b9-4958-b8c5-1e0cf5a3d016", ComponentIds.ParseJsonString, 1, 1); // Parse JSON String
        yield return Gh1MigrationRule.Identity("b70e62d2-141b-48f9-b023-b0579178c22c", ComponentIds.GetJsonObjectKey, 2, 1); // Get JSON Object Key
        yield return Gh1MigrationRule.Identity("66bfbf37-4512-4f12-85b5-90003e224fb0", ComponentIds.ReadJsonArray, 1, 1); // Read JSON Array
        yield return Gh1MigrationRule.Identity("acc96ec4-b5d8-49fc-b8cf-2e604430388b", ComponentIds.ReadJsonObject, 1, 2); // Read JSON Object
        yield return Gh1MigrationRule.Identity("d59d2f8a-23d2-4e84-9a80-1ac245ea57e5", ComponentIds.ReadJsonValue, 1, 3); // Read JSON Value
        yield return Gh1MigrationRule.Identity("034d6e3b-ae02-4d7b-995b-88f752f36790", ComponentIds.SeparateJsonTokens, 1, 3); // Separate JSON Tokens
        yield return Gh1MigrationRule.Identity("5c90048b-9a14-43b5-9996-c67511880604", ComponentIds.StringifyJsonToken, 1, 1); // Stringify JSON Token
    }
}
