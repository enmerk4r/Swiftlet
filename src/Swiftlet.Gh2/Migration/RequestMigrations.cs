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
        yield return Gh1MigrationRule.Identity("be0d2120-8576-44b1-934f-1a70568907a2", ComponentIds.CreateJsonArray, 1, 1); // Create JSON Array
        yield return Gh1MigrationRule.Identity("2892726b-fb0e-424a-877d-353f8ac18dc5", ComponentIds.CreateJsonObject, 2, 1); // Create JSON Object
        yield return Gh1MigrationRule.Identity("ca82e229-fcdb-415f-bd0b-5f37c1ef8c3f", ComponentIds.CreateJsonValue, 1, 1); // Create JSON Value
        yield return Gh1MigrationRule.Identity("0deca619-b911-42c3-906d-36f2bcd7a61e", ComponentIds.MergeJsonObjects, 3, 1); // Merge JSON Objects
        yield return Gh1MigrationRule.Identity("452cf748-8d55-42f8-b141-499b8f931891", ComponentIds.RemoveJsonKey, 2, 1); // Remove JSON Key
        yield return Gh1MigrationRule.Identity("306ee7f8-1c8e-4500-a3b9-8215b8934b43", ComponentIds.SetJsonKey, 3, 1); // Set JSON Key
        yield return Gh1MigrationRule.Identity("70f48e9d-e37a-4695-961b-3b653542448d", ComponentIds.CreateTextBodyCustom, 2, 1); // Create Text Body Custom
        yield return Gh1MigrationRule.Identity("43f12a67-6ab6-450d-8a10-63081f20cdbf", ComponentIds.CreateByteArrayBody, 2, 1); // Create Byte Array Body
        yield return Gh1MigrationRule.Identity("05df5f52-6e60-4492-9332-03189a83ec18", ComponentIds.CreateByteArrayBodyFromFile, 2, 1); // Create Byte Array Body from File
        yield return Gh1MigrationRule.Identity("c9d0e1f2-a3b4-2233-7e8f-90011223344a", ComponentIds.CreateFormUrlEncodedBody, 2, 1); // Create Form URL Encoded Body
        yield return Gh1MigrationRule.Identity("7df2ccdb-0d64-4ca4-b1b9-506b46b52338", ComponentIds.CreateMultipartFieldText, 3, 1); // Create Multipart Field Text
        yield return Gh1MigrationRule.Identity("1a6521b4-40da-40ad-bc43-9b067a9ea6a8", ComponentIds.CreateMultipartFieldBytes, 4, 1); // Create Multipart Field Bytes
        yield return Gh1MigrationRule.Identity("15c68c1c-9e2c-4df3-83f3-8d9fb1fcd5cf", ComponentIds.CreateMultipartFormBody, 1, 1); // Create Multipart Form Body
    }
}

/// <summary>
/// Create Text Body (current and archived GH1 versions) keeps its content-type menu setting. GH1 restores
/// it into the private _contentType field when the file is read; read it from the restored source.
/// </summary>
public abstract class TextBodyMigrationBase : IMigrateComponent
{
    public abstract Guid Grasshopper1Id { get; }

    public IDocumentObject Migrate(IGH_Component source)
    {
        var rule = Gh1MigrationRule.Identity(Grasshopper1Id.ToString(), ComponentIds.CreateTextBody, 1, 1);
        var target = (Swiftlet.Gh2.Components.Body.CreateTextBodyComponent)((IMigrateComponent)rule).Migrate(source);
        var field = source.UnderlyingObject.GetType().GetField("_contentType", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(source.UnderlyingObject) is not string contentType)
            throw new InvalidOperationException("Cannot migrate the saved Create Text Body content type.");
        target.ContentType = contentType;
        return target;
    }
}

public sealed class TextBodyMigration : TextBodyMigrationBase
{
    public override Guid Grasshopper1Id => new("d5bf7d4a-9fc3-4984-90f7-feb32aa96d9f");
}

public sealed class ArchivedTextBodyMigration : TextBodyMigrationBase
{
    public override Guid Grasshopper1Id => new("87d53fb2-ba5d-46dc-bc7b-92eac5aebc6c");
}
