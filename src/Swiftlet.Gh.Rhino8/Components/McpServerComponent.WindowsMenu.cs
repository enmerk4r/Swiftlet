using System.Windows.Forms;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class McpServerComponent
{
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Copy MCP Config", OnCopyMcpConfig, true);
    }

    private void OnCopyMcpConfig(object? sender, EventArgs e)
    {
        CopyConfigToClipboard();
    }
}
