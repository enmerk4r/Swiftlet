using System.Text.Json.Nodes;

namespace Swiftlet.Gh.Rhino8;

public static class ModernMcpResponseWorkflow
{
    public static bool TrySendResponse(
        ModernMcpToolCallContext request,
        JsonNode? content,
        bool isError)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.HasResponded)
        {
            return false;
        }

        JsonNode normalizedContent = JsonNodeCloner.Clone(content) ?? JsonValue.Create(string.Empty)!;

        return isError
            ? request.TryRespondWithError(-32000, ToErrorMessage(normalizedContent))
            : request.TryRespondWithJson(normalizedContent);
    }

    public static (ModernMcpToolCallContext Request, string ToolName, JsonObject Arguments) Deconstruct(
        ModernMcpToolCallContext request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return (request, request.ToolName, JsonNodeCloner.CloneObject(request.Arguments));
    }

    private static string ToErrorMessage(JsonNode content)
    {
        if (content is JsonValue value &&
            value.TryGetValue<string>(out string? text) &&
            text is not null)
        {
            return text;
        }

        return content.ToJsonString();
    }
}
