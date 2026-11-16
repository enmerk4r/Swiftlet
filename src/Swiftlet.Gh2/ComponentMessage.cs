using Grasshopper2.Components;

namespace Swiftlet.Gh2;

/// <summary>
/// The GH2 counterpart of GH1's component <c>Message</c> (the grey text under a component, e.g. a listener's
/// URI or a transfer's progress): <see cref="Component.Label"/>, which GH2 draws inside the bottom of the
/// capsule. Its setter is protected, so components pass it in as <paramref name="setLabel"/>.
/// The label changes the component's layout, so it is always applied on the UI thread; Process runs on
/// solver threads and transfer/listener callbacks on worker threads.
/// </summary>
internal static class ComponentMessage
{
    public static void Show(Component component, string? text, Action<string?> setLabel)
    {
        text = string.IsNullOrWhiteSpace(text) ? null : text;
        if (Rhino.RhinoApp.InvokeRequired)
            Rhino.RhinoApp.InvokeOnUiThread((Action)(() => Apply(component, text, setLabel)));
        else
            Apply(component, text, setLabel);
    }

    private static void Apply(Component component, string? text, Action<string?> setLabel)
    {
        if (!string.Equals(component.Label, text, StringComparison.Ordinal))
            setLabel(text);
    }
}
