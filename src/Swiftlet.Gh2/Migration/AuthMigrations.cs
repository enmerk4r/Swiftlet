using Grasshopper2.Doc.Migration;

namespace Swiftlet.Gh2.Migration;

public sealed class AuthMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return Gh1MigrationRule.Identity("6d601bf4-8302-44b6-8d64-85818fb9a419", ComponentIds.BasicAuth, 2, 1);
        yield return Gh1MigrationRule.Identity("133b9c3f-63b0-42e2-bc8e-8eb7d4e916bc", ComponentIds.BearerTokenAuth, 1, 1);
        yield return Gh1MigrationRule.Identity("09b3834f-0211-4339-a88e-738202f228fa", ComponentIds.ApiKeyAuth, 2, 2);
        yield return Gh1MigrationRule.Identity("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f", ComponentIds.OAuthToken, 7, 6);
        yield return Gh1MigrationRule.Identity("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e", ComponentIds.OAuthAuthorize, 5, 4);
    }
}
