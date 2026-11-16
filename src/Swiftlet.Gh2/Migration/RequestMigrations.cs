using System.Reflection;
using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Components;

namespace Swiftlet.Gh2.Migration;

/// <summary>GH1 -> GH2 migrations for the "2. Request" tab.</summary>
public sealed class RequestMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("7dc20210-c08f-466f-af5e-286f70f4c630", ComponentIds.CreateHttpHeader, 2, 1);
        yield return Gh1MigrationRule.Identity("ba9be0b0-aecc-49d4-95c0-491c00cabecb", ComponentIds.CreateQueryParam, 2, 1);
        yield return Gh1MigrationRule.Identity("104e0b1e-9954-475c-8046-a7259a8967f5", ComponentIds.CreateUrl, 4, 1);
    }
}

