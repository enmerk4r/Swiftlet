using System.Reflection;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Gh.Rhino8;

public sealed class RhinoClipboardService : IClipboardService
{
    public Task<HostActionResult> SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            Type? clipboardType = ResolveClipboardType();
            if (clipboardType is null)
            {
                return Task.FromResult(HostActionResult.Manual(
                    "Clipboard service is not available.",
                    "Copy the generated MCP config manually from the component status or logs."));
            }

            object? clipboard = clipboardType.GetProperty("Instance")?.GetValue(null);
            PropertyInfo? textProperty = clipboardType.GetProperty("Text");

            if (clipboard is null || textProperty is null || !textProperty.CanWrite)
            {
                return Task.FromResult(HostActionResult.Manual(
                    "Clipboard service is not available.",
                    "Copy the generated MCP config manually from the component status or logs."));
            }

            textProperty.SetValue(clipboard, text);
            return Task.FromResult(HostActionResult.Success("MCP configuration copied to clipboard."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HostActionResult.Manual(
                $"Clipboard copy failed: {ex.Message}",
                "Copy the generated MCP config manually from the component status or logs."));
        }
    }

    private static Type? ResolveClipboardType()
    {
        Type? type = Type.GetType("Eto.Forms.Clipboard, Eto", throwOnError: false);
        if (type is not null)
        {
            return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType("Eto.Forms.Clipboard", throwOnError: false);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
