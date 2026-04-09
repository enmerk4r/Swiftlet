using System.Windows.Forms;
using Swiftlet.Core.Mcp;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class McpServerComponent
{
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        Menu_AppendSeparator(menu);

        ToolStripMenuItem configMenu = new("MCP Config");
        configMenu.DropDownItems.Add(CreateConfigMenuItem("Copy Claude Desktop config", McpClientConfigTarget.ClaudeDesktop));
        configMenu.DropDownItems.Add(CreateConfigMenuItem("Copy LM Studio config", McpClientConfigTarget.LmStudio));
        configMenu.DropDownItems.Add(CreateConfigMenuItem("Copy VS Code config", McpClientConfigTarget.VsCode));
        configMenu.DropDownItems.Add(CreateConfigMenuItem("Copy Claude Code config", McpClientConfigTarget.ClaudeCode));
        configMenu.DropDownItems.Add(CreateConfigMenuItem("Copy Codex config", McpClientConfigTarget.Codex));
        menu.Items.Add(configMenu);
    }

    private void OnCopyMcpConfig(object? sender, EventArgs e)
    {
        McpClientConfigTarget target = sender is ToolStripMenuItem { Tag: McpClientConfigTarget taggedTarget }
            ? taggedTarget
            : McpClientConfigTarget.ClaudeDesktop;

        CopyConfigToClipboard(target);
    }

    private ToolStripMenuItem CreateConfigMenuItem(string label, McpClientConfigTarget target)
    {
        return new ToolStripMenuItem(label, null, OnCopyMcpConfig)
        {
            Tag = target,
        };
    }
}
