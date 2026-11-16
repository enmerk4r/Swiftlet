using Grasshopper2.Doc.Migration;
namespace Swiftlet.Gh2.Migration;
public sealed class McpMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("77ff2424-16e0-4642-9e94-88933557b56f", ComponentIds.CreateMcpTextContent, 1, 1);
        yield return Gh1MigrationRule.Identity("3e0f8ff7-f684-465c-a46a-f74dddef1df2", ComponentIds.CreateMcpResourceLink, 5, 1);
        yield return Gh1MigrationRule.Identity("4e1e3ffb-0592-4de3-a3aa-1adb000526da", ComponentIds.CreateMcpEmbeddedTextResource, 3, 1);
        yield return Gh1MigrationRule.Identity("327f8c2d-3964-41a6-bbb6-994441616a3c", ComponentIds.CreateMcpEmbeddedBinaryResource, 3, 1);
        yield return Gh1MigrationRule.Identity("b33bef39-9140-4881-a562-eac060946c5d", ComponentIds.CreateMcpImageContent, 2, 1);
        yield return Gh1MigrationRule.Identity("f6a7b8c9-d0e1-2345-f012-456789012345", ComponentIds.DefineToolParameter, 4, 1);
        yield return Gh1MigrationRule.Identity("e5f6a7b8-c9d0-1234-ef01-345678901234", ComponentIds.DefineTool, 3, 1);
        yield return Gh1MigrationRule.Identity("a7b8c9d0-e1f2-3456-0123-567890123456", ComponentIds.DeconstructToolCall, 1, 3);
        yield return Gh1MigrationRule.Identity("08814cbb-0f3d-44cc-b3d8-41b2053101de", ComponentIds.McpToolResponse, 4, 1);
    }
}
