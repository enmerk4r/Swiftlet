using System.Text.Json.Nodes;

namespace Swiftlet.Core.Json;

public static class JsonObjectMerger
{
    public const JsonObjectMergeConflictMode DefaultConflictMode = JsonObjectMergeConflictMode.PreferB;

    public static JsonObject Merge(
        JsonObject? objectA,
        JsonObject? objectB,
        JsonObjectMergeConflictMode mode = DefaultConflictMode)
    {
        JsonObject result = CloneObject(objectA);

        if (objectB is null)
        {
            return result;
        }

        foreach ((string key, JsonNode? valueB) in objectB)
        {
            if (!result.TryGetPropertyValue(key, out JsonNode? valueA))
            {
                result[key] = Clone(valueB);
                continue;
            }

            result[key] = MergeNodes(valueA, valueB, mode);
        }

        return result;
    }

    public static bool TryParseConflictMode(int rawMode, out JsonObjectMergeConflictMode mode)
    {
        switch (rawMode)
        {
            case 0:
                mode = JsonObjectMergeConflictMode.PreferA;
                return true;
            case 1:
                mode = JsonObjectMergeConflictMode.PreferB;
                return true;
            case 2:
                mode = JsonObjectMergeConflictMode.PreferAUnlessNull;
                return true;
            case 3:
                mode = JsonObjectMergeConflictMode.PreferBUnlessNull;
                return true;
            default:
                mode = DefaultConflictMode;
                return false;
        }
    }

    private static JsonNode? MergeNodes(
        JsonNode? valueA,
        JsonNode? valueB,
        JsonObjectMergeConflictMode mode)
    {
        if (valueA is JsonObject objectA && valueB is JsonObject objectB)
        {
            return Merge(objectA, objectB, mode);
        }

        if (AreEquivalent(valueA, valueB))
        {
            return Clone(valueA);
        }

        return ResolveConflict(valueA, valueB, mode);
    }

    private static JsonNode? ResolveConflict(
        JsonNode? valueA,
        JsonNode? valueB,
        JsonObjectMergeConflictMode mode)
    {
        return mode switch
        {
            JsonObjectMergeConflictMode.PreferA => Clone(valueA),
            JsonObjectMergeConflictMode.PreferB => Clone(valueB),
            JsonObjectMergeConflictMode.PreferAUnlessNull => valueA is null ? Clone(valueB) : Clone(valueA),
            JsonObjectMergeConflictMode.PreferBUnlessNull => valueB is null ? Clone(valueA) : Clone(valueB),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported JSON merge conflict mode."),
        };
    }

    private static JsonNode? Clone(JsonNode? node)
    {
        return node is null ? null : JsonNode.Parse(node.ToJsonString());
    }

    private static JsonObject CloneObject(JsonObject? node)
    {
        return node is null ? [] : (JsonNode.Parse(node.ToJsonString()) as JsonObject) ?? [];
    }

    private static bool AreEquivalent(JsonNode? left, JsonNode? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left is JsonObject leftObject && right is JsonObject rightObject)
        {
            if (leftObject.Count != rightObject.Count)
            {
                return false;
            }

            foreach ((string key, JsonNode? leftValue) in leftObject)
            {
                if (!rightObject.TryGetPropertyValue(key, out JsonNode? rightValue) || !AreEquivalent(leftValue, rightValue))
                {
                    return false;
                }
            }

            return true;
        }

        if (left is JsonArray leftArray && right is JsonArray rightArray)
        {
            if (leftArray.Count != rightArray.Count)
            {
                return false;
            }

            for (int index = 0; index < leftArray.Count; index++)
            {
                if (!AreEquivalent(leftArray[index], rightArray[index]))
                {
                    return false;
                }
            }

            return true;
        }

        return string.Equals(left.ToJsonString(), right.ToJsonString(), StringComparison.Ordinal);
    }
}
