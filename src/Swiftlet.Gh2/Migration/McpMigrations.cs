using Grasshopper2.Doc.Migration;
using Grasshopper2.Doc;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Parameters;
namespace Swiftlet.Gh2.Migration;
public sealed class McpMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return new McpParameterMigration("1b8787e7-c778-457f-9022-78b776009db6", () => new McpContentBlockParameter());
        yield return new McpParameterMigration("c9d0e1f2-a3b4-5678-2345-789012345678", () => new McpToolParameterParameter());
        yield return new McpParameterMigration("d0e1f2a3-b4c5-6789-3456-890123456789", () => new McpToolDefinitionParameter());
        yield return new McpParameterMigration("e1f2a3b4-c5d6-7890-4567-901234567890", () => new McpToolCallRequestParameter());
        yield return Gh1MigrationRule.Identity("77ff2424-16e0-4642-9e94-88933557b56f", ComponentIds.CreateMcpTextContent, 1, 1);
        yield return Gh1MigrationRule.Identity("3e0f8ff7-f684-465c-a46a-f74dddef1df2", ComponentIds.CreateMcpResourceLink, 5, 1);
        yield return Gh1MigrationRule.Identity("4e1e3ffb-0592-4de3-a3aa-1adb000526da", ComponentIds.CreateMcpEmbeddedTextResource, 3, 1);
        yield return Gh1MigrationRule.Identity("327f8c2d-3964-41a6-bbb6-994441616a3c", ComponentIds.CreateMcpEmbeddedBinaryResource, 3, 1);
        yield return Gh1MigrationRule.Identity("b33bef39-9140-4881-a562-eac060946c5d", ComponentIds.CreateMcpImageContent, 2, 1);
        yield return Gh1MigrationRule.Identity("f6a7b8c9-d0e1-2345-f012-456789012345", ComponentIds.DefineToolParameter, 4, 1);
        yield return Gh1MigrationRule.Identity("e5f6a7b8-c9d0-1234-ef01-345678901234", ComponentIds.DefineTool, 3, 1);
        yield return Gh1MigrationRule.Identity("a7b8c9d0-e1f2-3456-0123-567890123456", ComponentIds.DeconstructToolCall, 1, 3);
        yield return Gh1MigrationRule.Identity("b8c9d0e1-f2a3-4567-1234-678901234567", ComponentIds.McpToolResponseArchived, 3, 1);
        yield return Gh1MigrationRule.Identity("08814cbb-0f3d-44cc-b3d8-41b2053101de", ComponentIds.McpToolResponse, 4, 1);
    }
}

internal sealed class McpParameterMigration : IMigrateParameter
{
    private readonly Func<Grasshopper2.Parameters.IParameter> _create;
    public McpParameterMigration(string id, Func<Grasshopper2.Parameters.IParameter> create)
    { Grasshopper1Id = new Guid(id); _create = create; }
    public Guid Grasshopper1Id { get; }
    public IDocumentObject Migrate(IGH_Param source)
    { var target = _create(); source.Transfer(target); return target; }
}
