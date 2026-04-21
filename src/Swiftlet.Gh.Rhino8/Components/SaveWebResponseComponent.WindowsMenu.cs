using System.Windows.Forms;

namespace Swiftlet.Gh.Rhino8.Components;

public sealed partial class SaveWebResponseComponent
{
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalMenuItems(menu);
        Menu_AppendItem(menu, "Text", MenuTextClick, true, _textChecked);
        Menu_AppendItem(menu, "Binary", MenuBinaryClick, true, _binaryChecked);
    }

    private void MenuTextClick(object sender, EventArgs args)
    {
        UncheckAll();
        _textChecked = true;
        UpdateMessage();
    }

    private void MenuBinaryClick(object sender, EventArgs args)
    {
        UncheckAll();
        _binaryChecked = true;
        UpdateMessage();
    }
}
