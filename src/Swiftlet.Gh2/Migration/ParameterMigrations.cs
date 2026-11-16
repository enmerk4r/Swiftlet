using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Grasshopper2.Parameters;
using Swiftlet.Gh2.Parameters;

namespace Swiftlet.Gh2.Migration;

// Grasshopper 1 -> Grasshopper 2 rules for free-floating data-type parameters (a Swiftlet parameter
// placed on the canvas by itself). Parameters that belong to a component are carried by that
// component's rule. Listener Request, WebSocket Connection and the MCP types are in the Server tables.
public sealed class ParameterMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return new ParameterMigration<HttpHeaderParameter>("521dae84-7074-433e-9714-9144c42b92a4"); // Http Header
        yield return new ParameterMigration<QueryParameterParameter>("f59b2397-a996-48b3-8aa3-6b8d56f6244f"); // Query Param
        yield return new ParameterMigration<HttpResponseDataParameter>("b1c8d5fd-72ad-4f80-9ada-5f3850bc1a94"); // Http Response
        yield return new ParameterMigration<RequestBodyParameter>("cc4b9260-48dc-432e-80c8-5ebf9ab3f66e"); // Request Body
        yield return new ParameterMigration<MultipartFieldParameter>("f5e60c2f-7d03-4a68-b117-848005d73bbc"); // Multipart Field
        yield return new ParameterMigration<JsonNodeParameter>("a2bdb76c-7a0e-4537-a7f1-0fb37a6b35ac"); // JSON Token
        yield return new ParameterMigration<JsonObjectParameter>("90f38432-a460-43e2-a1ac-747d8ca6236c"); // JSON Object
        yield return new ParameterMigration<JsonArrayParameter>("645a1994-d22d-4103-877e-849f96811291"); // JSON Array
        yield return new ParameterMigration<JsonValueParameter>("f46d2a50-02f2-46c9-8a93-f7a8d37843d4"); // JSON Value
        yield return new ParameterMigration<HtmlNodeParameter>("2ac72237-3718-4bcb-aefc-79ca13c9fad1"); // HTML Node
        yield return new ParameterMigration<XmlNodeParameter>("3bc83348-4829-5cdc-bffd-80db24d0fbe2"); // XML Node
        yield return new ParameterMigration<BitmapParameter>("c1cdd3a5-faaf-484a-978b-307139ebf7f4"); // Bitmap
        yield return new ParameterMigration<ByteArrayParameter>("8c021fd2-744d-4f13-bbe4-94467cf397a1"); // Byte Array
    }
}

/// <summary>Replaces a Grasshopper 1 parameter with <typeparamref name="TParameter"/>, keeping name, state and wires.</summary>
internal sealed class ParameterMigration<TParameter> : IMigrateParameter
    where TParameter : class, IParameter, new()
{
    public ParameterMigration(string grasshopper1Id) => Grasshopper1Id = new Guid(grasshopper1Id);

    public Guid Grasshopper1Id { get; }

    public IDocumentObject Migrate(IGH_Param source)
    {
        var target = new TParameter();
        source.Transfer(target);
        return target;
    }
}
