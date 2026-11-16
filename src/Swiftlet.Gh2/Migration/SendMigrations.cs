using System.Reflection;
using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Components;

namespace Swiftlet.Gh2.Migration;

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


public sealed class SendMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return new RequestMigration("18931e0e-bb79-4863-8e0f-4a0f13a137a6", ComponentIds.PostRequest, 4);
        yield return new RequestMigration("8192683a-a7a2-4c59-84ab-93c1f066b52e", ComponentIds.PutRequest, 4);
        yield return new RequestMigration("2ebe4cc3-334b-4e80-bd1d-59e66e70da46", ComponentIds.PatchRequest, 4);
        yield return new RequestMigration("50043941-dc41-4d1f-8dd8-7cb529c27152", ComponentIds.DeleteRequest, 3);
        yield return new RequestMigration("cc8b1ecf-a2e7-495c-8d41-1e84011633bf", ComponentIds.HttpRequest, 5);
        yield return Gh1MigrationRule.Identity("4c0bdeaa-e4c2-4c7d-b139-358c23e39a96", ComponentIds.DeconstructHeader, 1, 2);
        yield return Gh1MigrationRule.Identity("4a7f8985-730a-45da-803d-d5c21ef1ea0e", ComponentIds.DeconstructQueryParam, 1, 2);
        yield return Gh1MigrationRule.Identity("6240a6fa-f6fa-47af-a796-0a2fc3dd6cc1", ComponentIds.DeconstructUrl, 1, 5);
        yield return Gh1MigrationRule.Identity("d5e6f7a8-b9c0-1234-5678-90abcdef1234", ComponentIds.DeconstructBody, 1, 3);
        yield return Gh1MigrationRule.Identity("6b60cc1e-e463-4997-b1b3-7d1835ec0c0b", ComponentIds.DeconstructHttpResponse, 1, 7);
        yield return Gh1MigrationRule.Identity("8f2ea76a-117e-40db-a25f-466870c3c79f", ComponentIds.DeconstructMultipartField, 1, 5);
        yield return Gh1MigrationRule.Identity("f38c3ec8-a611-46c3-8c79-a1b38de62d00", ComponentIds.DeconstructMultipartBody, 1, 1);
    }

    private sealed class RequestMigration : IMigrateComponent
    {
        private readonly string _target;
        private readonly int _inputs;
        public Guid Grasshopper1Id { get; }
        public RequestMigration(string legacy, string target, int inputs)
        { Grasshopper1Id = new Guid(legacy); _target = target; _inputs = inputs; }
        public IDocumentObject Migrate(IGH_Component source)
        {
            var target = (HttpRequestComponentBase)((IMigrateComponent)Gh1MigrationRule.Identity(
                Grasshopper1Id.ToString(), _target, _inputs, 3)).Migrate(source);
            var property = source.UnderlyingObject.GetType().GetProperty("TimeoutSeconds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(source.UnderlyingObject) is not int timeout)
                throw new InvalidOperationException("Cannot migrate the saved HTTP timeout setting.");
            target.TimeoutSeconds = timeout;
            return target;
        }
    }
}
