using Grasshopper2.Doc.Migration;

namespace Swiftlet.Gh2.Migration;

public sealed class AuthMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("6d601bf4-8302-44b6-8d64-85818fb9a419", ComponentIds.BasicAuth, 2, 1);
        yield return Gh1MigrationRule.Identity("133b9c3f-63b0-42e2-bc8e-8eb7d4e916bc", ComponentIds.BearerTokenAuth, 1, 1);
        yield return Gh1MigrationRule.Identity("09b3834f-0211-4339-a88e-738202f228fa", ComponentIds.ApiKeyAuth, 2, 2);
    }
}
