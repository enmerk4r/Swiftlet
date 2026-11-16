using Grasshopper2.Components;
using Grasshopper2.Doc;
using Grasshopper2.Doc.Migration;
using Grasshopper2.Interop;
using Swiftlet.Gh2.Components;
using Swiftlet.Gh2.Parameters;

namespace Swiftlet.Gh2.Migration;

public sealed class ServerMigrationTable : IMigrationTable
{
    public IEnumerable<IMigrateBase> CreateMigrators()
    {
        yield return new ServerInputMigration();
        yield return new ListenerRequestMigration();
        foreach (var rule in NetworkMigrationRules.Create())
            yield return rule;
    }
}

internal sealed class ServerInputMigration : IMigrateComponent
{
    public Guid Grasshopper1Id => new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public IDocumentObject Migrate(IGH_Component source)
    {
        var target = new ServerInputComponent();
        for (int index = 1; index < source.OutputCount; index++)
            target.DoCreateParameter(Side.Output, index, null!);
        source.TransferObjectName(target);
        source.TransferObjectState(target);
        source.TransferInputs(target, [(0, 0)]);
        source.TransferOutputs(target, Enumerable.Range(0, source.OutputCount).Select(index => (index, index)).ToArray());
        // GH1 route identity lives in NickName; generic output transfer preserves wiring, not this domain setting.
        var parameters = source.UnderlyingObject.GetType().GetProperty("Params")!.GetValue(source.UnderlyingObject)!;
        var outputs = (System.Collections.IList)parameters.GetType().GetProperty("Output")!.GetValue(parameters)!;
        for (int index = 0; index < outputs.Count; index++)
        {
            object output = outputs[index]!;
            target.Parameters.Output(index).UserName = (string)output.GetType().GetProperty("NickName")!.GetValue(output)!;
        }
        target.VariableParameterMaintenance();
        return target;
    }
}

internal sealed class ListenerRequestMigration : IMigrateParameter
{
    public Guid Grasshopper1Id => new("c8ede657-11f0-460e-9485-68b507d7668d");
    public IDocumentObject Migrate(IGH_Param source)
    {
        var target = new ListenerRequestParameter();
        source.Transfer(target);
        return target;
    }
}
