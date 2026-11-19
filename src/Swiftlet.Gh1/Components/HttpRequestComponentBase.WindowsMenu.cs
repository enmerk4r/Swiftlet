using System.Windows.Forms;

namespace Swiftlet.Gh.Rhino8.Components;

public abstract partial class HttpRequestComponentBase
{
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);

        ToolStripMenuItem timeoutMenu = new("Timeout");

        foreach (int timeout in TimeoutOptions)
        {
            string label = timeout >= 60
                ? $"{timeout / 60} min" + (timeout % 60 > 0 ? $" {timeout % 60} s" : "")
                : $"{timeout} s";

            if (timeout == 100)
            {
                label += " (default)";
            }

            ToolStripMenuItem item = new(label, null, OnTimeoutClick)
            {
                Tag = timeout,
                Checked = TimeoutSeconds == timeout,
            };
            timeoutMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(timeoutMenu);
    }

    private void OnTimeoutClick(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is int timeout)
        {
            TimeoutSeconds = timeout;
            ExpireSolution(true);
        }
    }
}
