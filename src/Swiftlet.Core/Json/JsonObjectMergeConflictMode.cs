namespace Swiftlet.Core.Json;

public enum JsonObjectMergeConflictMode
{
    PreferA = 0,
    PreferB = 1,
    PreferAUnlessNull = 2,
    PreferBUnlessNull = 3,
}
