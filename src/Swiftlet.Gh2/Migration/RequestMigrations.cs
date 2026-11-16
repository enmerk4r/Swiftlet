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

public sealed class GetRequestMigration : IMigrateComponent
{
    public Guid Grasshopper1Id => new("f2c68f1e-862d-40c0-8bd1-30153ec28c03");

    public IDocumentObject Migrate(IGH_Component source)
    {
        var rule = Gh1MigrationRule.Identity(Grasshopper1Id.ToString(), "3bad80b0-77e4-4c32-b35c-d0a73ff6bf59", 3, 3);
        var target = (GetRequestComponent)((IMigrateComponent)rule).Migrate(source);
        // GH1 Read has already restored the serialized setting. Use its protected
        // property without referencing Grasshopper.dll from the native GH2 shell.
        var property = source.UnderlyingObject.GetType().GetProperty("TimeoutSeconds",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.GetValue(source.UnderlyingObject) is not int timeout)
            throw new InvalidOperationException("Cannot migrate the saved GET timeout setting.");
        target.TimeoutSeconds = timeout;
        return target;
    }
}

